# Set Up Navigation

Wire navigation between pages in a StellarUI MAUI application.

## Instructions

Navigation in StellarUI uses observable pipelines, not imperative method calls.
Always attach navigation subscriptions inside `Bind(WeakCompositeDisposable disposables)`.

### Basic Navigation (Push)

```csharp
// Navigate on button tap:
_nextButton
    .Events().Clicked
    .Select(static _ => Unit.Default)
    .NavigateToPage<${TargetPage}>(this)
    .DisposeWith(disposables);
```

### Navigate with Query Parameters

```csharp
// The target ViewModel must have [QueryParameter] on the receiving property.
// e.g., in DetailViewModel:
//   [QueryParameter]
//   [Reactive]
//   private int _itemId;

_itemTapped
    .NavigateToPage<int, ${DetailPage}>(
        this,
        queryParameters: static (itemId, dict) => dict.Add("ItemId", itemId))
    .DisposeWith(disposables);
```

### Navigate Back

```csharp
// Pop current page:
_backButton
    .Events().Clicked
    .Select(static _ => Unit.Default)
    .NavigateBack(this)
    .DisposeWith(disposables);
```

### Modal Navigation

```csharp
// Push as modal:
_openModalButton
    .Events().Clicked
    .Select(static _ => Unit.Default)
    .NavigateToModalPage<${ModalPage}>(this)
    .DisposeWith(disposables);

// Pop modal:
_closeButton
    .Events().Clicked
    .Select(static _ => Unit.Default)
    .NavigateModalBack(this)
    .DisposeWith(disposables);
```

### Popup Navigation (Mopups)

```csharp
// Push popup:
_openPopupButton
    .Events().Clicked
    .Select(static _ => Unit.Default)
    .NavigateToPopupPage<${PopupPage}>(this)
    .DisposeWith(disposables);

// Pop popup:
_closePopupButton
    .Events().Clicked
    .Select(static _ => Unit.Default)
    .NavigatePopupBack(this)
    .DisposeWith(disposables);
```

### Navigate from ListView Tapped

```csharp
_itemsListView
    .ItemTapped<${ItemViewModel}>(deselectAfterTap: true)
    .Select(static item => item.Id)
    .NavigateToPage<int, ${DetailPage}>(
        this,
        queryParameters: static (id, dict) => dict.Add("Id", id))
    .DisposeWith(disposables);
```

### Navigate from ViewModel Command

```csharp
// In the ViewModel's Bind():
OpenDetailCommand = ReactiveCommand
    .CreateFromObservable<int, Unit>(id =>
        Observable.Empty<Unit>()
            .NavigateToPage<int, DetailPage>(
                this,
                queryParameters: static (id, dict) => dict.Add("Id", id)))
    .DisposeWith(disposables);
```

## Key Rules
- All navigation is **extension methods on `IObservable<T>`** — not service method calls
- Navigation is **automatically throttled** (~204ms) to prevent double-tap
- Target pages are resolved from **DI** — they must be registered with `[ServiceRegistration]`
- Navigation runs on `RxSchedulers.MainThreadScheduler`; page creation on background thread
- `[QueryParameter]` on ViewModel property is required to receive passed parameters
- Subscribe **in `Bind()`** and **always** `.DisposeWith(disposables)`
