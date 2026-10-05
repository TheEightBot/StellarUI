# MAUI recycling review follow-ups: research and resolution (2026-10-05)

Base for all work: `origin/fix/maui-recycling-follow-ups` at `c087e45` (PR #54, which is
`origin/develop` `b73c2a4` plus one commit). Line numbers for this repo refer to that commit
unless a branch is named.

Primary sources used (all read at the pinned versions, not from memory):

- dotnet/maui at commit `6c379bf1dc24461a985a093be3fd8a3e9bc6fc00`, the commit recorded in
  `microsoft.maui.controls.core/10.0.110/*.nuspec` (`<repository ... commit="6c379bf..."/>`).
  Paths below are relative to that checkout.
- reactiveui/reactiveui at commit `9e7839c2034cc61d98f908bdf644363b9a6cc214`, recorded in
  `reactiveui.reactive/24.3.0/*.nuspec`.
- androidx/androidx `RecyclerView.java` on `androidx-main` at commit `a293caeb` (the
  `recyclerview` module; the Xamarin binding in the NuGet cache is 1.4.0.x). The lines quoted
  have been stable across 1.x.
- dotnet/runtime at tag `v10.0.0`: `WeakReference.T.cs`, `ConditionalWeakTable.cs`.
- Microsoft Learn: friend assemblies, `InternalsVisibleToAttribute`, strong naming guidance,
  strong-named assemblies (URLs in Finding 3).
- Shipped binaries in the NuGet cache, read with `System.Reflection.Metadata`
  (`Microsoft.Maui.Controls.dll` 10.0.110, `ReactiveUI.Reactive.dll` and
  `ReactiveUI.Maui.Reactive.dll` 24.3.0).

Verification environment note: partway through the session the machine's .NET installs were
changed by something outside this session (Homebrew linked `dotnet` 10.0.401 at 16:57, and the
`/usr/local/share/dotnet` install lost its 10.0.400-band workload manifests at about 17:00:
`dotnet workload` reports "Workload set version 10.0.400.1 has missing manifests"). The
documented override (`-p:TargetFrameworks=net10.0 -p:TargetFramework=net10.0`) stopped
working because the MAUI workload could no longer be resolved. Finding 1 was fully verified
with the documented override before that happened. Finding 2 (and the merged state of both
branches) was verified with an equivalent workload-free build of the same `net10.0` head:
`-p:UseMaui=false -p:SingleProject=false
-p:CustomBeforeMicrosoftCommonTargets=<scratchpad>/no-platforms.targets`, where the targets
file only removes `Platforms/**` from `Compile`. It compiles the same sources against the same
`Microsoft.Maui.Controls` 10.0.110 package. Nothing on the machine was repaired or modified by
this session.

---

## Finding 1: lifecycle asymmetry when a parked view's view model is replaced

**Verdict: solved** on `fix/maui-replaced-view-model-lifecycle` at `b8c839f`, contained in
`Stellar.Maui`, no core change, no per-recycle allocation.

### What the code does today

`Stellar.Maui/MauiViewManager.cs` (base `c087e45`):

- `PropertyChanged` on window regained (lines 51-64): `Unpark(out parkedViewModel)`; if the
  parked view model is still `isv.ViewModel` the view is re-attached without tearing down
  (53-59); otherwise `Deactivate(isv, parkedViewModel)` (61) then `HandleActivated(isv)` (64).
- `Deactivate` (180-190): `HandleDeactivated(view)` first, then `replaced.Unregister()` for the
  parked view model if it differs from `view.ViewModel`, then `view.DisposeView()`.
- `IParkedItemView.DeactivateParked` (90-98) is the same `Deactivate(view, parkedViewModel)`
  call, reached from `ItemsHostWatcher.ReleaseParked` when the list leaves its window, the
  limit is lowered, or a new view arrives.
- `Stellar/ViewManager.cs` `HandleDeactivated` (284-291) calls `OnLifecycle(view, Deactivated)`
  then `UnregisterBindings(view)`; `OnLifecycle` (304-319) notifies `view.ViewModel` if it is
  `ILifecycleEventAware`, then pushes to the view-level subject. So in the replaced case the
  *replacement* receives `Deactivated` and the original receives nothing but `Unregister()`.
- While the view is parked, `cell.ViewModel = replacement` runs `ViewManager.PropertyChanged`
  (293-302) which calls `SetupViewModel(view.ViewModel)` on the replacement (Initialize +
  Register only, no lifecycle event).

### (a) Sequences observed today (verified by running a characterisation test on `c087e45`)

Using `GridDataModelCell` with `TestCellViewModel : ILifecycleEventAware` from
`Stellar.Maui.UnitTests/Support`, events recorded per view model and on
`ViewManager.LifecycleEvents`:

| Scenario | view stream | original VM | replacement VM |
|---|---|---|---|
| A. bind, recycle (parked), replace VM, rebind | `Deactivated, Initialized, Activated, Attached` | `Initialized, Activated, Attached, Detached` then nothing | `Deactivated, Initialized, Activated, Attached` (BindCalls 2) |
| B. bind, recycle, replace VM, list leaves window | `Deactivated` | nothing after `Detached` | `Deactivated` |
| C. outside a list: attach, replace VM, remove from page | `Detached, Deactivated` | nothing after `Attached`; **stays Registered** | `Detached, Deactivated` |

So the original never receives `Deactivated` and the replacement receives one without ever
having been activated. The view-level stream is already right (exactly one `Deactivated`).

Failing tests written first (all four fail on `c087e45`, verified):
`RecycledView_WhoseViewModelWasReplacedMeanwhile_DeactivatesTheViewModelItActivatedWhenItReturns`,
`..._WhenTheListLeavesTheWindow`,
`RecycledView_WhoseViewModelWasTakenAwayMeanwhile_DeactivatesTheViewModelItActivated`,
`RecycledView_WhoseReplacedViewModelIsNoLongerAlive_RaisesDeactivatedOnceAndOnlyOnTheView`
(`Stellar.Maui.UnitTests/ItemsViewRecyclingTests.cs`).

### (b) What v2.1.3 did, and the other heads

- `git show v2.1.3:Stellar.Maui/MauiViewManager.cs`: on window lost it called
  `OnLifecycle(isv, Detached)`, `HandleDeactivated(isv)`, `isv.DisposeView()`; there was no
  parking, so the view model present at that moment received `Deactivated`.
- `git diff v2.1.3 HEAD -- Stellar/ViewManager.cs Stellar/Extensions/IViewForExtensions.cs`
  is empty: core's `PropertyChanged` -> `SetupViewModel` and `OnLifecycle` -> `view.ViewModel`
  are unchanged since v2.1.3. The only contract that existed is "the event goes to whatever
  view model the view holds at that moment"; there is no hand-off to the view model that was
  activated.
- Scenario C above is that pre-existing gap on MAUI outside lists (verified by running). At
  core level a throwaway test in `Stellar.UnitTests` (`HandleActivated`, swap `ViewModel`,
  `PropertyChanged("ViewModel")`, `HandleDeactivated`) gave
  `original=[Initialized, Activated] replacement=[Deactivated]` (verified by running).
- Avalonia/WinUI/WPF/Uno managers (`Stellar.Avalonia/AvaloniaViewManager.cs`,
  `Stellar.WinUI/WinUIViewManager.cs`, `Stellar.Wpf/WpfViewManager.cs`,
  `Stellar.Uno/UnoViewManager.cs`) override `HandleActivated`/`HandleDeactivated` only to
  manage hot reload and call the base; their view bases forward `ViewModel` property changes
  to `ViewManager.PropertyChanged` (for example `Stellar.Avalonia/UserControlBase.cs:94-108`).
  They therefore share the gap (inferred from reading; not run, those heads do not build in
  the slnf test job on this machine).
- ReactiveUI's own contract is the opposite: `ViewForMixins.HandleViewModelActivation`
  (`src/ReactiveUI.Shared/Activation/ViewForMixins.cs` at `9e7839c`, lines ~470-495) disposes
  the previous view model's activation (`viewModelDisposable.Disposable =
  EmptyDisposable.Instance`) before calling `Activate()` on the new one whenever the view's
  `ViewModel` changes while the view is active. That is the behaviour the fix adopts for the
  parked path.

The general gap outside lists is left alone: fixing it in core means remembering the activated
view model per `ViewManager<TViewModel>` and changing event delivery on every head, which is a
behaviour change outside this finding's scope. It is recorded here so it can be scheduled.

### (c) The fix

`DeactivateParked(view, parkedViewModel)` (branch, `MauiViewManager.cs`): if the parked view
model is still the view's, the normal `Deactivate` runs. Otherwise:

1. `OnLifecycle(parkedViewModel, Deactivated)`: a new protected overload on core's
   `ViewManager<TViewModel>` that takes the view model to notify instead of reading
   `view.ViewModel`. The public `OnLifecycle(view, ...)` now forwards to it. The parked
   view model is notified and the subject is pushed exactly once, and the replacement is
   not notified. If the parked view model has been collected (`null`), nobody is notified
   and the stream still emits once.
2. `UnregisterBindings(view)` clears the control bindings and unregisters the replacement
   (which `SetupViewModel` had registered), exactly as before.
3. `parkedViewModel.Unregister()` for the original, as before.
4. `view.DisposeView()`, as before.

Allocation: none. An ordinary recycle or rebind does not reach this path either.

Why a core member: the view-level subject is private to core and only reachable through
`OnLifecycle(view, ...)`, which always notifies `view.ViewModel`. The research branch first
avoided touching core with a private stand-in `IStellarView<TViewModel>` whose `ViewModel`
was the parked one; it worked, but it allocated on that path, threw `NotSupportedException`
from every other member, and would break whenever the interface grows. A protected overload
that adds no state and no virtual call is the smaller change. The rejected alternative, a
`protected virtual TViewModel? LifecycleViewModel(view)` hook, would put a virtual call on
every lifecycle event of every view. Trade-off to know about: on this one path the virtual
`HandleDeactivated` is not called; nothing in the repo overrides it on `MauiViewManager`,
but a consumer subclass that does would not see the replaced-view-model deactivation.

Results after the change (documented override, before the environment changed):
`dotnet build Stellar.slnf -c Release` 0 errors; `Stellar.UnitTests` 259 passed;
`Stellar.Maui.UnitTests` 202 passed (198 + 4). New sequences: original
`..., Detached, Deactivated`; replacement `Initialized, Activated, Attached`; view stream
unchanged.

---

## Finding 2: allocations under the performance mandate

**Verdict: solved** on `perf/maui-recycling-allocations` at `8d8d9a0`, with one conscious
behaviour change (late opt-in takes effect from each row's next bind), documented in the test,
README and XML docs.

### What the code does today

- `MauiViewManager.TrackItemsHost` (110-127): on the first `Parent` that is an `ItemsView`,
  `new WeakReference<ItemsView?>(host)` for every item view of every list, then
  `Find(host)?.ReleaseParked()`; later parents only `SetTarget`.
- `TryPark` (129-160): checks `Maintain`, view model, binding context, cached host and its
  window, then `ItemsHostWatcher.Find(host)` (a `ConditionalWeakTable` lookup on every recycle
  of a list in a window), then `_parking ??= new Parking(this)` (one object plus three
  `WeakReference`s: `Owner`, `_view`, `_viewModel`) *before* `watcher.TryPark` can refuse.
- `ItemsHostWatcher` (`Stellar.Maui/ItemsHostWatcher.cs`): `Lock _gate` (14),
  `Volatile` on `_limit` (34-39), `TryPark`/`Unpark`/`ReleaseParked` under the lock.

### What MAUI does on recycle and bind (the reason the host is cached)

`src/Controls/src/Core/Element/Element.cs`:

- `AddLogicalChild` (215-226) -> `OnChildAdded` (659-669) -> `child.SetParent(this)`;
  `RemoveLogicalChild` (236-255, 273-278) -> `OnChildRemoved` (679-689) -> `child.SetParent(null)`.
- `SetParent` (396-449): `OnPropertyChanging(nameof(Parent))` (405); `RealParent = value`
  (422); binding-context inheritance (432-440); `OnParentSet()` (441), whose base
  implementation (694-699) calls `PropagatePropertyChanged(null)`; only then
  `OnPropertyChanged(nameof(Parent))` (448).
- `PropertyPropagationExtensions.cs` 21-30 and 120-134: `PropagatePropertyChanged(null, ...)`
  calls `SetWindowFromParent(element)` -> `controller.Window = child.Parent?.Window`.
- `VisualElement.cs` 481-496: `Window` is a read-only bindable property; setting it raises
  `OnPropertyChanged("Window")` through `BindableObject` and `OnWindowChanged` (2423).

Consequence, confirmed in source: when a view is removed from a list, `RealParent` is already
`null` when `Window` becomes `null` and the `"Window"` notification fires, and the `"Parent"`
notification fires afterwards. When a view is added, `RealParent` is already the list when the
`"Window"` notification fires. So the list a view is *leaving* can only be known from state
remembered at bind time; the view manager cannot read it at recycle time.

Per platform at 10.0.110:

- Android: `Handlers/Items/Android/Adapters/ItemsViewAdapter.cs` `OnViewRecycled` (53-61)
  -> `TemplatedItemViewHolder.Recycle` (35-43) -> `itemsView.RemoveLogicalChild(View)`;
  `OnBindViewHolder` (63-74) -> `Bind` (45-88): new content gets `BindingContext` (68), then
  `PropagatePropertyChanged(null, View, itemsView)` (71: window without a parent, which is the
  `FirstBind.PropagateWindowThenAddToList` case in the harness), then `AddLogicalChild` (87);
  reused content gets `BindingContext` (84) then `AddLogicalChild` (87). The pool is cleared
  without telling the adapter in `MauiRecyclerView.cs` 209, 281, 1065, 1074.
- iOS/Mac Catalyst (default handler is `CollectionViewHandler2`, `Hosting/AppHostBuilderExtensions.cs`
  66-69): `Handlers/Items2/iOS/TemplatedCell2.cs` `Unbind` (92-102) sets `BindingContext = null`
  and `RemoveLogicalChild`; it is called only from `ItemsViewController2.CellDisplayingEndedFromDelegate`
  (732-751) when the cell's item is no longer at its index (removed from the source), and on
  teardown. Routine reuse goes through `BindVirtualView` (254-305): `SetValueFromRenderer(BindingContextProperty, ...)`
  (296) and `AddLogicalChild` only if `view.Parent is null` (297-300). So on iOS the
  parent/window round trip happens on item removal and teardown, not on every scroll reuse.
  The legacy `TemplatedCell.Unbind` (`Handlers/Items/iOS/TemplatedCell.cs` 84-98) clears only
  the binding context; `DetachFromItemsView` (100-111) is teardown-only, by comment.
- Windows: `Platform/Windows/CollectionView/ItemContentControl.cs` `Realize` (159-240):
  `RemoveLogicalChild` first (168-171), return if there is no data context (173-176), then
  `BindingContext = dataContext` and `AddLogicalChild` (195-196 new, 213-217 reused). Window
  null and window regained happen inside one `Realize` call.

The harness (`Stellar.Maui.UnitTests/Support/ListHarness.cs`) makes exactly these calls.

AndroidX (`RecyclerView.java`, `androidx-main` @ `a293caeb`): `RecycledViewPool.DEFAULT_MAX_SCRAP = 5`
(6659); `Recycler.recycleViewHolderInternal` falls through to
`addViewHolderToRecycledViewPool(holder, true)` (7598-7601), which calls `dispatchViewRecycled`
-> `mAdapter.onViewRecycled(holder)` (7632-7650, 7874-7885) *before* `putRecycledView`, and
`putRecycledView` drops the holder when `mMaxScrap <= scrapHeap.size()` (6791-6796) with no
further adapter callback. That is why a parked slot can belong to a view the pool has already
discarded, and why slots are weak and released when a new view arrives.

### Options evaluated for 2(a)

1. **Skip the allocation when `Find(host)` is null.** Zero cost for non-opted lists; opted-in
   rows still allocate; late opt-in degrades (see below).
2. **Hold the `ItemsHostWatcher` instead of the host (chosen).** The watcher is created once
   per opted-in list and holds the list through one `WeakReference<ItemsView>`; it already
   subscribes to the list's `PropertyChanged`. A manager holding it strongly pins ~100 bytes
   that do not reference the list or any view. Zero per-row allocation for every list, no
   lookup at recycle. A static sentinel `ItemsHostWatcher.None` records "my list has not opted
   in" without a reference and without a bool field (which would have added 8 bytes per manager
   after padding). `Find` short-circuits on a static counter until any list has opted in, so
   an app that never calls `RecycledItemViewLimit` pays one static read per bind.
3. **Strong reference to the host.** Rejected. MAUI deliberately holds `RealParent` through a
   `WeakReference<Element>` allocated on every set (`Element.cs` 335, 366-376), so a rooted
   child does not pin its parent tree. A strong `view -> manager -> host` edge would undo that
   for every Stellar item view: any row kept alive (a `Maintain` row, a row whose bindings are
   held by a long-lived source while teardown did not run) would pin the list, its
   `ItemsSource`, its other children and its handler. The cycle `list -> child -> manager ->
   list` is fine for the GC; the leak amplification from an external root is the problem.
4. **Per-list handle for all lists** (watcher object for every list hosting Stellar rows,
   subscribing only on opt-in). Keeps immediate late opt-in and zero per-row cost, but gives
   every list an object plus a `WeakReference` plus a CWT entry, and reverses PR #54's "a list
   that has not opted in is not watched". Not chosen; it is the alternative if immediate late
   opt-in must be kept.
5. **Hook `OnParentChanging`/`OnPropertyChanging`** (the parent is still readable there,
   `Element.cs` 405-409). Needs every Stellar view base class to override and forward, and
   custom views implementing `IStellarView` directly would silently stop parking. Rejected.

Late opt-in (PR #54's `RecycledItemViewLimit_SetAfterViewsAreBound_KeepsThemFromTheirNextRecycle`):
with option 2 a row bound before the list opted in holds `None`, so its first recycle after
opt-in tears it down and its next bind picks up the watcher. The test is consciously changed to
`..._KeepsThemOnceTheyHaveBeenBoundAgain` (asserts the teardown, then the keep after rebind);
README and the XML remarks say to set the limit before the list shows its rows. Cost of the
change: one extra teardown/rebind per already-realised row, once, only when the limit is
raised at runtime; the recommended construction-time call is unaffected.

### 2(b): `TryPark` order

`ItemsHostWatcher.TryPark(IParkedItemView view, ref WeakReference<IParkedItemView>? slot)`
checks the list's window and the limit first and creates the slot's weak reference only when it
takes the view, inside the same single lock acquisition; the manager builds `Parking` around the
returned slot afterwards (`parking ??= _parking = new Parking(slot!)`). A view refused by a
full list, or by a list out of its window, allocates nothing. The slot is reused on later parks
of the same manager, so the pattern is still one allocation set per view that is ever kept.
Verified by reading and by the suite (over-limit and out-of-window tests pass); the saving
itself is not measurable in isolation because the refusal is immediately followed by the
teardown's allocations.

### Lock and Volatile

Call paths into `TryPark`/`Unpark`/`ReleaseParked`/`Limit`: `MauiViewManager.PropertyChanged`
(window lost/regained; raised by `BindableObject` from `Element.SetParent`, UI thread);
`TrackItemsHost` on a new view (UI thread); `ItemsHostWatcher.OnHostPropertyChanged` (the
list's own `Window` change, UI thread); `ItemsViewExtensions.RecycledItemViewLimit` (app code;
it mutates an `ItemsView`, which MAUI does not make thread-safe either); `MauiViewManager.Dispose`
(app code only: there are no finalizers in `Stellar`/`Stellar.Maui`, and nothing in the
repo calls `ViewManager.Dispose` from a finalizer or background thread). So no path in the
library runs off the UI thread; the lock and the volatile accesses only guard against app code
disposing a manager or setting the limit off-thread. An uncontended `Lock.Enter` is a CAS; the
volatile reads are plain loads on arm64/x64. Kept, because the cost is nanoseconds per park
and `ReleaseParked`'s snapshot-under-lock is what makes re-entrant rebinding during release
safe (`ViewReboundWhileRecycledViewsAreBeingReleased_KeepsItsBindings`).

### Measurements

`GC.GetAllocatedBytesForCurrentThread()` around the operations, Release, net10.0, after warm-up,
200 rows / 2000 cycles (harness in the scratchpad, `ZzAllocationMeasure.cs`, not committed):

| | base `c087e45` | branch `8d8d9a0` |
|---|---|---|
| first bind, per row, list not opted in | 3990 B | 3966 B (3 runs: 3966, 3966, 3990) |
| first bind, per row, list opted in (5) | 3990 B | 3966-3990 B |
| recycle+rebind cycle, not opted in (bindings torn down and rebuilt) | 21307 B | 21307-21331 B |
| recycle+rebind cycle, opted in (kept) | 3048 B | 3048-3072 B |
| `RecycledItemViewLimit(0)` on a never-opted list | 0 B | 0 B |

The 24 B run-to-run noise comes from CWT compaction inside `WeakCompositeDisposable` depending
on GC timing, so the coarse harness only suggests the saving. The committed test
`ItemView_RemembersItsListWithoutAllocating` isolates it: a manager's first
`PropertyChanged("Parent")`+`PropertyChanged("Window")` for a view inside a list allocates
**24 B on base (verified: the test fails with "Expected 0, Actual 24" against `c087e45`) and
0 B on the branch**, for both a non-opted and an opted-in list. The bytes understate the saving:
`WeakReference<T>` also allocates a GC handle (`WeakReference.T.cs` `Create`, line 81-83,
`GCHandle.InternalAlloc(..., GCHandleType.Weak)`) and has a finalizer (`~WeakReference()`,
line 173), both per item view, both now gone. For scale, MAUI itself allocates a
`WeakReference<Element>` on every `RealParent` set (every bind), so the per-view saving is
small next to MAUI's own per-bind cost; it is the GC handle and finalizer per row that the
mandate cares about.

Results: `dotnet build Stellar.slnf -c Release` 0 errors; `Stellar.UnitTests` 259 passed;
`Stellar.Maui.UnitTests` 201 passed (198 base, +1 allocation fact, +2 allocation theory
cases; the late opt-in test rewritten). The two allocation cases and the rewritten late
opt-in test fail against the base implementation (verified). Both branches merge cleanly on
top of each other and the merged tree passes 205 MAUI tests (verified on a throwaway branch,
deleted).

---

## Finding 3: `InternalsVisibleTo` ships in the package

**Verdict: no change recommended; leave the item as it is.** No branch was created because
there is nothing to implement.

### What the code does today

`Stellar.Maui/Stellar.Maui.csproj:42` `<InternalsVisibleTo Include="Stellar.Maui.UnitTests" />`,
unconditional; no `SignAssembly`/key anywhere in the repo. Internals reachable:
`MauiCommandRebinding`, `ItemsHostWatcher`, `IParkedItemView`, `HotReloadService.ClearCache/UpdateApplication`,
`PickerExtensions.TitleSelectorConverter`, `NavigationObservableExtensions.RequiredNavigationRoot`.
Tests that need it: `MauiCommandRebindingTests.cs` (7 tests new up the internal type) and
two PR #54 tests calling `ItemsHostWatcher.Find`. `HotReloadSubscriptionTests` uses reflection
on a public type, not IVT.

### What the primary sources say

- Microsoft Learn, "Friend assemblies" (https://learn.microsoft.com/dotnet/standard/assembly/friend):
  IVT is listed as the mechanism for "unit testing, when test code runs in a separate
  assembly"; "If Assembly A is not strong named, the friend assembly name should consist of
  only the assembly name."
- `InternalsVisibleToAttribute` reference
  (https://learn.microsoft.com/dotnet/api/system.runtime.compilerservices.internalsvisibletoattribute):
  "Both the current assembly and the friend assembly must be unsigned, or both assemblies must
  be signed with a strong name."
- "Strong-named assemblies" (https://learn.microsoft.com/dotnet/standard/assembly/strong-named):
  "Do not rely on strong names for security. They provide a unique identity only." and "For
  .NET Core and .NET 5+, strong-named assemblies do not provide material benefits. The runtime
  never validates the strong-name signature".
- "Strong naming and .NET libraries" (https://learn.microsoft.com/dotnet/standard/library-guidance/strong-naming):
  "Strong naming has no benefits on .NET Core/5+. C# compiler produces CS8002 warning for
  strong-named assemblies referencing non-strong named assemblies." and "DO NOT add, remove, or
  change the strong naming key" (it changes the assembly identity).
- Shipped binaries (verified by reading metadata): `Microsoft.Maui.Controls.dll` 10.0.110 is
  not strong-named (empty public key, `StrongNameSigned` flag off) and carries 48 IVT
  attributes by simple name, including `Microsoft.Maui.Controls.Core.UnitTests`,
  `Microsoft.Maui.Controls.DeviceTests`, `CommunityToolkit.Maui.UnitTests` and
  `DynamicProxyGenAssembly2`. `ReactiveUI.Reactive.dll` 24.3.0 is not strong-named and ships
  IVT to `ReactiveUI.Reactive.Tests`, `ReactiveUI.Wpf.Tests.Reactive`,
  `ReactiveUI.WinForms.Tests.Reactive`; the reactiveui repo at `9e7839c` declares them as
  unconditional `<InternalsVisibleTo>` items (`src/ReactiveUI.Reactive/ReactiveUI.Reactive.csproj`
  109-111, `src/ReactiveUI.Core/ReactiveUI.Core.csproj` 59-75 with the comment "IVT is
  tests-only").

### Actual exposure

- Security: none. Accessibility is a compile-time check; the .NET runtime has no code-access
  security, and reflection reaches internals regardless. The docs above say strong names are
  identity, not security.
- Consumers: anyone who names an assembly `Stellar.Maui.UnitTests` can compile against the
  internals and takes on an unsupported dependency. Nobody can do so by accident.
- Trimming/AOT: no documented effect; the attribute is not a trimmer root (inferred from the
  absence of any such rule in the trimming docs; not tested).
- Reference assemblies: a ref assembly keeps internals when IVT is present, but the package
  ships `lib/` implementation assemblies, not `ref/`, so nothing changes for consumers.
- Package: one attribute string in the assembly.

### Options

1. Leave as is. Matches MAUI and ReactiveUI; zero risk; compile-time-checked tests.
2. Condition the item so packed builds omit it. A `Configuration` condition breaks CI, which
   runs `dotnet test ... --configuration Release` (`.github/workflows/ci.yml:115-116`). The CLI
   sets a private `_IsPacking=true` property when packing (SDK comment in
   `Microsoft.NET.Sdk.DefaultItems.targets:173`, "_IsPacking will only be set if packing in
   the CLI"), and `nuget.yml` packs with `dotnet pack --configuration Release` after a plain
   `dotnet build`, so `Condition="'$(_IsPacking)' != 'true'"` would work today, as would a
   custom property passed only by `nuget.yml`. Either creates a release-only build flavour that
   CI never compiles, which is the exact failure mode recorded for v2.1.3 (the release job
   failed on a project CI had not built, after other packages were already pushed). Not worth
   it for an attribute with no exposure.
3. Strong-name. Rejected: no benefit on .NET 10 per the guidance, CS8002 against unsigned
   `Microsoft.Maui.Controls` and `ReactiveUI.*` (warnings are errors here), and it changes the
   assembly identity for every consumer.
4. Rewrite the tests to public API only. `MauiCommandRebindingTests` could resolve the
   registered `ICreatesCustomizedCommandRebinding` through the locator instead of `new`, but
   the two `ItemsHostWatcher.Find` tests state an internal fact ("no watcher exists") that has
   no public observable; they would need reflection on MAUI's private `PropertyChanged` field
   or on Stellar's type by name, which is strictly worse than IVT (runtime failure instead of
   compile error on rename). The `perf/` branch adds `RecycledItemViewLimit_OfZero_OnAListThatHasNotOptedIn_AllocatesNothing`,
   which expresses PR #54's intent through public API, but the `Find` tests still have value.

Recommendation: option 1, recorded here. If the owner wants the attribute gone on principle,
option 4 plus deleting the two `Find` tests is the only route that does not add a release-only
build; it should be done as its own change.

---

## What remains unverified

- Finding 1 on the Avalonia/WinUI/WPF/Uno heads is inferred from reading their view managers
  and view bases; only MAUI and the core `ViewManager<T>` were run.
- Finding 2's platform sequences are taken from MAUI source and reproduced by the harness; no
  device or emulator run was made. The AndroidX lines are from `androidx-main`, not the exact
  1.4.0 tag.
- The 24-byte number is the managed object only; GC handle and finalizer costs were not
  measured, only established from the runtime source.
- Finding 2 and the merged state were verified with the workload-free build described at the
  top because the machine's MAUI workload was removed mid-session; Finding 1 was verified with
  the documented override before that.
- Trimming effect of IVT: from documentation absence, not from an ILLink run.
