# StellarUI Performance Optimization Checklist (Draft)

> Temporary working document for performance-focused refactors on branch `perf-review`.
> This file is intended to be edited as we go and removed/archived before merging.

## Scope

-   Focus on the `Stellar` core library first, then platform-specific implementations in `Stellar.Maui`, `Stellar.Blazor`, and `Stellar.Avalonia`.
-   Prefer micro-optimizations that reduce allocations, closures, and GC pressure without sacrificing clarity or changing public APIs unnecessarily.
-   Each checklist item should correspond to one or more small, focused commits.

---

## Checklist

1. **`ViewManager<TViewModel>` observables and lifecycle/navigation pipelines**

    - [x] Review `Initialized`, `Activated`, `Attached`, `IsAppearing`, `IsDisappearing`, `Detached`, `Deactivated`, `Disposed`, `LifecycleEvents`, `NavigatedTo`, `NavigatedFrom`, and `NavigationEvents` for repeated query construction and lambda allocations.
    - [x] Where beneficial and safe, factor common query shapes into cached observables or helper methods to reduce per-subscription allocations.
    - [x] Confirm that no unnecessary allocations occur when events are unused (respecting lazy subject creation and `_disposed` gating).

2. **Disposable infrastructure (`WeakCompositeDisposable`, `WeakSerialDisposable`, `WeakSingleAssignmentDisposable`)**

    - [x] Audit for avoidable allocations in enumeration and internal collections.
    - [x] Remove or refactor lambda/closure usages in hot paths where they capture state unnecessarily.
    - [x] Identify internal helper types that can safely be `struct`/`readonly struct` without affecting public APIs.
    - [x] Verify disposal patterns do not retain references longer than necessary and aid GC by clearing collections promptly.

3. **Observable utilities (`Observables`, constant observables, and helpers)**

    - [x] Confirm `Observables.UnitDefault` is used in place of repeated `Observable.Return(Unit.Default)` where appropriate.
    - [x] Identify and introduce additional shared observables (e.g., `Observable.Empty<T>`, `Observable.Never<T>`) where usage patterns justify caching.
    - [x] Ensure new shared observables are thread-safe and do not introduce unintended global state.

4. **Attributes and reflection caches (`AttributeCache`, `ServiceRegistrationAttribute`, `PreCacheAttribute`, `QueryParameterAttribute`)**

    - [x] Review `AttributeCache` for repeated reflection calls or temporary allocations that can be cached or reduced.
    - [x] Consider using value-based keys (`struct`/`ValueTuple`) for internal cache keys when it reduces allocations and stays internal.
    - [x] Ensure any static caches avoid capturing lambdas with unnecessary closures.
    - [x] Validate that reflection caching does not leak types or assemblies and behaves well across reloads.

5. **String and observable extension methods (`StringExtensions`, `NotifyPropertyExtensions`, `IObservableExtensions`, `IStellarViewExtensions`, `IViewForExtensions`, etc.)**

    - [x] Identify extension methods used in hot paths that allocate lambdas or intermediate collections.
    - [x] Prefer static local functions or method group conversions over capturing lambdas where it removes allocations without harming clarity.
    - [x] Replace small, hot-path LINQ usage with simple loops where it is measurably beneficial and remains readable.

6. **View-model base and validation (`ViewModelBase`, `SelectionViewModel<T>`, `ValidatingViewModelBase<TNeedsValidation>`)**

    - [x] Inspect for repeated allocations during property change handling or validation (e.g., new lists/dictionaries on each change).
    - [x] Identify small internal data carriers that could safely become `struct`/`readonly struct`.
    - [x] Ensure validation pipelines and reactive subscriptions avoid capturing `this` where not required.

7. **`ValidationResult` and related result types**

    - [x] Evaluate `ValidationResult` (and any related result types) for safe migration from `class` to `struct` or `readonly struct`.
    - [x] Confirm no reliance on reference identity, inheritance, or mutability that would make `struct` semantics risky.
    - [x] If migration is safe, convert and adjust call sites to avoid unintended copying or boxing.

8. **Lifecycle and navigation event types (`LifecycleEvent`, `NavigationEvent`)**

    - [x] Verify they are used in allocation-friendly ways (no unnecessary new delegates per subscription).
    - [x] Where repeated event filters exist, centralize them via helper methods or pre-created observables.
    - [x] Ensure these helpers remain clear and maintain existing semantics.

9. **Disk cache implementation (`Stellar.DiskDataCache/DiskCache.cs`)**

    - [x] Audit read/write paths for large temporary allocations (e.g., `byte[]`, `MemoryStream`) and consider reuse/pooled patterns where straightforward.
    - [x] Review string/path handling to avoid unnecessary concatenations or `new` objects.
    - [x] Keep changes conservative and focused on obvious allocation hotspots.

10. **Platform-specific view managers and bindings (`MauiViewManager`, `BlazorViewManager`, `AvaloniaViewManager`)**

    - [x] Review per-navigation/per-render code paths for lambda/closure allocations and repeated observable or task chains.
    - [x] Cache simple, reusable delegates or helper observables where appropriate.
    - [x] Ensure no change in user-facing behavior or platform-specific semantics.

11. **General closure and allocation audit in `Stellar` core**

    - [x] Perform a pass over `Stellar/Extensions` and `Stellar/ViewModel` to identify obvious closure allocations and unnecessary LINQ in hot paths.
    - [x] Introduce small, well-named helpers where they reduce duplication and allocations.
    - [x] Avoid premature micro-optimizations; focus on clear wins.

12. **GC and disposal hygiene across core types**
    - [x] Confirm all disposables (`ViewManager<TViewModel>`, weak disposables, etc.) correctly release references and call `GC.SuppressFinalize` where applicable.
    - [x] Ensure event subscriptions and delegates are unsubscribed/cleared during disposal to prevent leaks.
    - [x] Add or refine XML docs/comments only where it clarifies lifecycle and disposal expectations (optional).

---

## Process Notes

-   Each checked item (or tight subset) should correspond to one or more focused commits on branch `perf-review`.
-   This document is expected to evolve; we can refine, re-order, or add/remove items as we learn from the code.
-   Before merging back to `main`, this file should be either removed or converted into a permanent design note if desired.
