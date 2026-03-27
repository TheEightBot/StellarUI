# Stellar Core — Performance & Design Review

> **Scope**: `Stellar/` project only (core framework library)
> **Goal**: Reduce allocations, improve throughput, modernize patterns — minimal API surface changes
> **Date**: 2026-03-27

---

## 1. Allocation Reduction

### 1.1 WeakCompositeDisposable — List allocation on every Clear/Dispose

**File**: `Stellar/Disposables/WeakCompositeDisposable.cs` (lines 138–161, 230–256)

Both `Clear()` and `Dispose()` allocate a `new List<IDisposable>()` every call to capture disposables before releasing the lock. In hot paths (view navigation), this creates GC pressure.

- [ ] **Use `Span<T>` / `stackalloc` for small counts, rent from `ArrayPool<IDisposable>` for larger collections**. A threshold of ~8 items can use stack allocation; above that, rent an array from the shared pool and return it after disposal. This eliminates the `List<T>` heap allocation entirely.

### 1.2 WeakCompositeDisposable — Lock object allocation

**File**: `Stellar/Disposables/WeakCompositeDisposable.cs` (line 14)

The `_gate` field is `new object()` — a heap allocation. The sibling classes `ViewManager` and `ViewModelBase` already use the .NET 9 `Lock` struct.

- [ ] **Replace `private readonly object _gate = new object()` with `private readonly Lock _gate = new()` across all three Weak* disposable types**. The `Lock` struct provides the same semantics with zero heap allocation and better JIT inlining.

### 1.3 WeakSerialDisposable / WeakSingleAssignmentDisposable — Same lock object issue

**Files**: `Stellar/Disposables/WeakSerialDisposable.cs` (line 13), `WeakSingleAssignmentDisposable.cs` (line 13)

- [ ] **Same fix as 1.2** — replace `object _gate` with `Lock _gate` in both types.

### 1.4 ViewManager — Observable property chains rebuild on every access

**File**: `Stellar/ViewManager.cs` (lines 20–42)

Every property access (e.g., `Initialized`, `Activated`) rebuilds the full `Where().SelectUnit().AsObservable()` chain. While Rx defers execution, the intermediate objects are allocated each time the property getter runs. Subscribers typically access these once, but hot-path lifecycle code could hit them repeatedly.

- [ ] **Cache each filtered observable in a `Lazy<IObservable<Unit>>` field, initialized in the constructor** (similar to how `_lifecycleEvents` is already lazy). Return `Observable.Empty<Unit>()` if disposed. This eliminates repeated intermediate operator allocations.

### 1.5 ValidationResult.DefaultValidationResult — Mutable static field

**File**: `Stellar/ValidationResult.cs` (line 15)

`DefaultValidationResult` is a `static` (non-readonly) field that holds a mutable `List<ValidationInformation>`. A consumer could inadvertently mutate the shared default. Additionally, using `new List<>()` for an always-empty collection wastes 56 bytes.

- [ ] **Make it `static readonly` and use `Array.Empty<ValidationInformation>()` wrapped in a read-only collection**. This ensures immutability and avoids the List allocation. Consider a pattern like:
  ```csharp
  public static readonly ValidationResult DefaultValidationResult =
      new(Array.Empty<ValidationInformation>(), true);
  ```
  Pair this with changing the `ICollection<ValidationInformation>` property to `IReadOnlyCollection<ValidationInformation>` (see section 3.2).

### 1.6 ValidatingViewModelBase — ValidationErrors Clear+Add loop

**File**: `Stellar/ViewModel/ValidatingViewModelBase.cs` (lines 112–119)

`UpdateValidationState` calls `Clear()` then `Add()` in a loop on an `ObservableCollection`. Each `Add` fires `CollectionChanged`, generating N events for N errors. Downstream subscribers (like `MonitorValidationInformationFor`) react to every single event.

- [ ] **Batch the update**: either suppress `CollectionChanged` during the loop and fire a single `Reset` event, or replace the collection contents in one shot. A simple approach is to add a helper that temporarily suppresses notifications, or to use a `BatchingObservableCollection` pattern.

### 1.7 IStellarViewExtensions — Uncached reflection call

**File**: `Stellar/Extensions/IStellarViewExtensions.cs` (line 28)

`Attribute.GetCustomAttribute(stellarView.GetType(), typeof(ServiceRegistrationAttribute))` bypasses the `AttributeCache` that exists specifically for this purpose. This is called during every view initialization.

- [ ] **Replace with `AttributeCache.GetAttribute<ServiceRegistrationAttribute>(stellarView.GetType())`** to use the cached lookup path, consistent with `ViewModelBase`.

### 1.8 IServiceCollectionExtensions — interfaces.Any() enumeration

**File**: `Stellar/Extensions/IServiceCollectionExtensions.cs` (line 60)

`interfaces.Any()` enumerates the array to check if it's non-empty, then the `foreach` enumerates it again. `GetInterfaces()` returns an array, so `.Length > 0` is O(1).

- [ ] **Replace `if (interfaces.Any())` with `if (interfaces.Length > 0)` (or just remove the check — `foreach` on an empty array is a no-op)**. Also remove the null coalescing `?? Enumerable.Empty<Type>()` since `GetInterfaces()` never returns null.

### 1.9 ObserveLatestOn — uses `object` lock instead of `Lock`

**File**: `Stellar/Extensions/IObservableExtensions.cs` (line 449)

Inside `ObserveLatestOn`, `var gate = new object()` is used for locking. This is in a hot scheduling path.

- [ ] **Replace with `var gate = new Lock()`** for consistency and the performance benefit of the .NET 9 Lock struct.

---

## 2. Thread Safety & Correctness

### 2.1 ViewManager — `_disposed` field race condition

**File**: `Stellar/ViewManager.cs` (lines 18, 97)

The `_disposed` field is a plain `bool` read without `Volatile.Read` in the observable property getters (lines 20–42) and in `OnLifecycle`/`OnNavigating`. Meanwhile, `Dispose` sets it outside the lock (line 97). This can lead to torn reads on ARM architectures.

- [ ] **Use `Volatile.Write(ref _disposed, true)` in `Dispose` and `Volatile.Read(ref _disposed)` in all reads** (matching the pattern already used for `_controlsBound`). Alternatively, wrap in a consistent property like `ViewModelBase.IsDisposed`.

### 2.2 ViewModelBase — `_isDisposed` lacks volatile access

**File**: `Stellar/ViewModel/ViewModelBase.cs` (line 38)

`IsDisposed => _isDisposed` is a direct field read, yet `_isDisposed` is set in `Dispose(bool)` potentially from another thread. `Initialized` and `BindingsRegistered` correctly use `Volatile.Read`, but `IsDisposed` does not.

- [ ] **Change to `public bool IsDisposed => Volatile.Read(ref _isDisposed);`** for consistency with the other volatile-read properties.

### 2.3 ViewModelBase — `Maintain` property has no synchronization

**File**: `Stellar/ViewModel/ViewModelBase.cs` (line 36)

`Maintain` is a simple auto-property read in `Unregister()` inside the lock but set from external code (e.g., `InitializeInternal`, `IStellarViewExtensions`) without synchronization. If `Maintain` is set from a different thread than `Unregister`, there's a data race.

- [ ] **Consider making `Maintain` use a volatile backing field or read it only inside the existing lock**. This is low-risk but worth being explicit about for correctness on weakly-ordered architectures.

### 2.4 WeakSingleAssignmentDisposable — `_isAssigned` race in DisposableContainer

**File**: `Stellar/Disposables/WeakSingleAssignmentDisposable.cs` (lines 170–176)

`SetDisposable` reads and writes `_isAssigned` non-atomically. While the outer `_gate` lock protects this today, the container class itself is not thread-safe if ever used outside the lock.

- [ ] **Use `Interlocked.CompareExchange` on an `int` flag** (matching the `Interlocked.Exchange` pattern used in `WeakSerialDisposable.DisposableContainer.SwapDisposable`). This makes the container inherently thread-safe and removes the reliance on external locking.

---

## 3. Design Pattern Improvements

### 3.1 ValidationInformation — `IsError` always false in 3-param constructor

**File**: `Stellar/ValidationResult.cs` (lines 41–47)

The constructor `ValidationInformation(string, string, object?)` hard-codes `IsError = false` even though it receives an `error` message string. This appears to be a bug — a validation information with an error message should likely have `IsError = true`.

- [ ] **Set `IsError = true` in the 3-parameter constructor** (and its 2-parameter forwarding constructor). Verify this against downstream consumer expectations before changing.

### 3.2 ValidationResult — Use IReadOnlyCollection for immutability

**File**: `Stellar/ValidationResult.cs` (line 7)

`ICollection<ValidationInformation>` allows mutation (Add/Remove/Clear) on a record type that should be immutable. This contradicts the record semantics and the `DefaultValidationResult` caching strategy.

- [ ] **Change to `IReadOnlyCollection<ValidationInformation>`** (or `IReadOnlyList<>`). This is an API surface change, but a correctness-motivated one — records should expose immutable data. Callers constructing results would pass arrays or lists (both implement `IReadOnlyCollection`).

### 3.3 RegisteredServiceNotFoundException — Unused `using System.Runtime.Serialization`

**File**: `Stellar/Exceptions/RegisteredServiceNotFoundException.cs` (line 3)

The `using System.Runtime.Serialization` is unused (no `[Serializable]` attribute, no serialization constructor).

- [ ] **Remove the unused using directive**.

### 3.4 StringExtensions.Contains — Redundant on .NET 9

**File**: `Stellar/Extensions/StringExtensions.cs` (lines 5–8)

`string.Contains(string, StringComparison)` has been a built-in BCL method since .NET Core 2.1. This extension method shadows the built-in and could cause confusion. Since the framework targets net9.0, it's purely redundant.

- [ ] **Remove `StringExtensions.Contains`** — callers already get the BCL overload. If this method is used directly via `StringExtensions.Contains(...)` anywhere, search and replace with the built-in call.

### 3.5 ValueIsTrue / ValueIsFalse — Semantic confusion with WhereIsTrue/WhereIsFalse

**File**: `Stellar/Extensions/IObservableExtensions.cs` (lines 106–114)

`ValueIsTrue` does `Select(static result => result)` which is an identity projection — it returns the value unchanged. `ValueIsFalse` inverts it. These names suggest filtering, but they're actually projections. Meanwhile `WhereIsTrue`/`WhereIsFalse` do the filtering. This is confusing.

- [ ] **Consider renaming to `Negate()`/`Identity()` or similar**, or add XML documentation that clearly differentiates these from the `Where*` variants. If these are unused, consider removing them.

### 3.6 ViewModelBase — Consider `sealed` disposal pattern

**File**: `Stellar/ViewModel/ViewModelBase.cs` (lines 100–119)

The class has `GC.SuppressFinalize(this)` in `Dispose()` but no finalizer. This is not harmful but is unnecessary overhead if no derived class adds a finalizer. More importantly, `_isDisposed = true` is set outside the `if (disposing)` block, which means a finalizer call (if one existed) would also set `_isDisposed`.

- [ ] **Move `_isDisposed = true` inside the `if (disposing)` block** since there are no unmanaged resources and no finalizer. This follows the modern simplified disposal pattern.

### 3.7 NotifyPropertyExtensions — Code duplication between scheduler/no-scheduler paths

**File**: `Stellar/Extensions/NotifyPropertyExtensions.cs` (lines 8–94)

Each of the three methods (`ObservePropertyChanged`, `ObservePropertyChanging`, `ObserveCollectionChanged`) has near-identical code duplicated in an `if (scheduler is not null)` / `else` branch. The only difference is passing `scheduler` as the last argument.

- [ ] **Refactor to use `null` for the scheduler parameter** — `Observable.FromEvent` accepts `null` scheduler gracefully (uses synchronous notification). If not, extract a local `Action<T>` factory and call once. This removes ~50 lines of duplication.

### 3.8 IServiceCollectionExtensions — Double attribute lookup

**File**: `Stellar/Extensions/IServiceCollectionExtensions.cs` (lines 19–29, 39–49)

`HasAttribute<ServiceRegistrationAttribute>(ti)` is called in the `Where` filter, then `GetAttribute<ServiceRegistrationAttribute>(ti)` is called again in the loop body. While `AttributeCache` makes this O(1) after the first call, the pattern is wasteful — the first call already retrieves the attribute.

- [ ] **Replace the Where + GetAttribute pattern with a Select + Where-not-null pattern**, e.g.:
  ```csharp
  assembly.ExportedTypes
      .Select(t => (Type: t, Attr: AttributeCache.GetAttribute<ServiceRegistrationAttribute>(t)))
      .Where(x => x.Attr != null && x.Type.IsAssignableTo(registrationType) && !x.Type.IsAbstract)
  ```
  This performs a single cache lookup per type instead of two.

---

## 4. Performance Optimizations

### 4.1 ObserveLatestOn — Replace Materialize/Accept with direct handling

**File**: `Stellar/Extensions/IObservableExtensions.cs` (lines 443–497)

`Materialize()` boxes every value into a `Notification<T>` object (heap allocation per item). For a method designed to handle backpressure efficiently, this is counterproductive.

- [ ] **Replace with a custom observer** that directly handles OnNext/OnError/OnCompleted without materialization. Store the latest value in a field with a `hasValue` flag instead of a `Notification<T>?`. This eliminates one allocation per emitted item.

### 4.2 ThrottleFirst — Complex operator chain

**File**: `Stellar/Extensions/IObservableExtensions.cs` (lines 499–540)

`ThrottleFirst` uses `Publish` + `Take(1)` + `Concat` + `IgnoreElements` + `TakeUntil` + `Delay` + `Repeat` + another `TakeUntil`. This creates a large operator graph with many intermediate subscriptions.

- [ ] **Consider a custom `IObservable<T>` implementation** using a simple timer-based gate (similar to how `Throttle` is implemented internally). A single `IScheduler.Schedule` call with a boolean gate would be far more efficient and easier to reason about. The allocation overhead of the current chain is significant for high-frequency sources.

### 4.3 SelectConcurrent/SelectSequential — Redundant Defer wrapping

**File**: `Stellar/Extensions/IObservableExtensions.cs` (lines 126–271)

All `SelectConcurrent` and `SelectSequential` overloads wrap with `Observable.Defer(() => Observable.Start(...))`. The `Defer` is unnecessary when wrapping `Observable.Start` which already defers execution. The extra `Defer` adds an allocation per item.

- [ ] **Remove the `Observable.Defer` wrapper** — `Observable.Start` already creates a cold observable that defers until subscription.

### 4.4 ValidatingViewModelBase.GetValidationInformation — LINQ FirstOrDefault on ObservableCollection

**File**: `Stellar/ViewModel/ValidatingViewModelBase.cs` (line 176)

`errors.FirstOrDefault(ni => ...)` uses LINQ, which allocates an enumerator on `ObservableCollection<T>`. Validation collections are typically small, but this is called on every `CollectionChanged` event.

- [ ] **Replace with a manual `for` loop** over the collection to avoid the enumerator allocation:
  ```csharp
  for (int i = 0; i < errors.Count; i++)
  {
      if (errors[i].PropertyName.Equals(propertyName, StringComparison.Ordinal))
          return errors[i];
  }
  return defaultValue;
  ```

---

## 5. Modernization Opportunities

### 5.1 ArgumentNullException.ThrowIfNull — Modern guard pattern

**Files**: Multiple (WeakCompositeDisposable, WeakSerialDisposable, WeakSingleAssignmentDisposable)

Manual `if (x == null) throw new ArgumentNullException(nameof(x))` patterns can be replaced with the .NET 6+ `ArgumentNullException.ThrowIfNull(x)` one-liner.

- [ ] **Replace all manual null-check-throw patterns** with `ArgumentNullException.ThrowIfNull()`. This is a minor modernization that improves readability and provides better stack traces (the method name is inlined by the JIT).

### 5.2 ObjectDisposedException.ThrowIf — Modern disposed guard

**File**: `Stellar/ViewManager.cs` (lines 100–106)

`ThrowIfDisposed()` manually checks `_disposed` and throws. .NET 7+ provides `ObjectDisposedException.ThrowIf(condition, instance)`.

- [ ] **Replace `ThrowIfDisposed()` with inline `ObjectDisposedException.ThrowIf(_disposed, this)`** calls. This removes the private helper method and is recognized by the JIT for better inlining.

### 5.3 Collection expressions — .NET 9 / C# 13

**Files**: Multiple

`new List<IDisposable>()`, `new List<ValidationInformation>()`, `Enumerable.Empty<Type>()` etc. can use collection expressions (`[]`) in C# 13 (which the project uses via `LangVersion=preview`).

- [ ] **Replace `new List<T>()` with `[]` and `Enumerable.Empty<T>()` with `[]`** where appropriate. The compiler optimizes `[]` to use pooled arrays for empty collections. For example:
  ```csharp
  // Before
  List<IDisposable> disposables = new();
  // After
  List<IDisposable> disposables = [];
  ```

### 5.4 `sealed` on leaf classes

**Files**: `SelectionViewModel<T>` and any other non-abstract, non-extended ViewModels

Classes that aren't designed for inheritance should be `sealed` to enable devirtualization optimizations by the JIT.

- [ ] **Audit all concrete (non-abstract) classes and add `sealed` where inheritance is not intended**. The weak disposable types are already `sealed` (good). `SelectionViewModel<T>` is not.

### 5.5 FrozenDictionary for AttributeCache (read-heavy, write-rare)

**File**: `Stellar/Extensions/AttributeCache.cs`

`ConcurrentDictionary` is optimized for concurrent reads and writes. After app startup, the attribute cache is almost exclusively read. `FrozenDictionary` (or `FrozenSet`) from .NET 8+ is optimized for read-heavy scenarios with zero contention overhead.

- [ ] **Consider a two-phase approach**: use `ConcurrentDictionary` during startup/registration, then "freeze" the cache into a `FrozenDictionary` via a `Freeze()` method called after DI configuration. This is a design consideration — only implement if profiling shows `ConcurrentDictionary` overhead in read paths.

### 5.6 `file`-scoped types for internal helpers

**Files**: `DisposableCollection` in WeakCompositeDisposable, `DisposableContainer` in WeakSerialDisposable / WeakSingleAssignmentDisposable

These nested private classes could use the `file` access modifier (C# 11+) if extracted to file scope, which gives slightly cleaner code. However, since they're already `private`, this is purely stylistic.

- [ ] **Optional / Low Priority**: Consider whether extracting to `file`-scoped types improves readability. No performance impact.

---

## 6. Summary Priority Matrix

| Priority | Item | Impact | Risk |
|----------|------|--------|------|
| 🔴 High | 1.7 — IStellarViewExtensions uncached reflection | Perf (every view init) | Very Low |
| 🔴 High | 2.1 — ViewManager `_disposed` volatile | Correctness (ARM) | Low |
| 🔴 High | 2.2 — ViewModelBase `_isDisposed` volatile | Correctness (ARM) | Low |
| 🔴 High | 3.1 — ValidationInformation `IsError` bug | Correctness | Medium |
| 🟡 Medium | 1.1 — WeakCompositeDisposable List alloc | Alloc reduction | Low |
| 🟡 Medium | 1.2/1.3 — Lock struct across Weak* types | Alloc reduction | Very Low |
| 🟡 Medium | 1.4 — ViewManager cached observables | Alloc reduction | Low |
| 🟡 Medium | 1.5 — DefaultValidationResult immutability | Correctness + Alloc | Low |
| 🟡 Medium | 1.6 — Batched ValidationErrors update | Perf (N events → 1) | Medium |
| 🟡 Medium | 4.3 — Remove redundant Defer wrapping | Alloc (per Rx item) | Low |
| 🟡 Medium | 4.4 — Manual loop vs LINQ FirstOrDefault | Alloc reduction | Very Low |
| 🟡 Medium | 5.2 — ObjectDisposedException.ThrowIf | Modernization | Very Low |
| 🟡 Medium | 3.8 — Double attribute lookup | Perf (startup) | Very Low |
| 🟢 Low | 1.8 — interfaces.Any() optimization | Minor perf | Very Low |
| 🟢 Low | 1.9 — ObserveLatestOn Lock struct | Alloc reduction | Very Low |
| 🟢 Low | 3.2 — IReadOnlyCollection for ValidationResult | API correctness | Medium (API) |
| 🟢 Low | 3.3 — Unused using directive | Cleanup | None |
| 🟢 Low | 3.4 — Redundant StringExtensions.Contains | Cleanup | Low |
| 🟢 Low | 3.7 — NotifyProperty code duplication | Maintainability | Low |
| 🟢 Low | 4.1 — ObserveLatestOn de-materialize | Alloc (advanced) | Medium |
| 🟢 Low | 4.2 — ThrottleFirst custom impl | Alloc (advanced) | Medium |
| 🟢 Low | 5.1 — ArgumentNullException.ThrowIfNull | Modernization | Very Low |
| 🟢 Low | 5.3 — Collection expressions | Modernization | Very Low |
| 🟢 Low | 5.4 — Seal leaf classes | JIT optimization | Very Low |
| ⚪ Future | 5.5 — FrozenDictionary for AttributeCache | Read perf | Medium |
| ⚪ Future | 5.6 — File-scoped types | Style | None |
| ⚪ Future | 2.3 — Maintain volatile backing | Correctness edge | Low |
| ⚪ Future | 2.4 — Interlocked in DisposableContainer | Defensive coding | Very Low |
| ⚪ Future | 3.5 — ValueIsTrue/ValueIsFalse naming | API clarity | Low (API) |
| ⚪ Future | 3.6 — Simplified disposal pattern | Correctness | Very Low |
