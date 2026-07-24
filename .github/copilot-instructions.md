# StellarUI — GitHub Copilot Instructions

## Project Identity

StellarUI is a **highly opinionated ReactiveUI-on-Rails** cross-platform MVVM framework built by Eight-Bot.
It wraps **ReactiveUI 21** with consistent lifecycle management, DI auto-registration, and C#-code-first UI construction across **.NET MAUI**, **Blazor**, and **Avalonia**.

Target framework: **net9.0** (MAUI targets also `net9.0-android`, `net9.0-ios`, `net9.0-maccatalyst`, `net9.0-windows10.0.19041.0`).
Language version: **C# latest / preview** — use top-level features freely (primary constructors, `partial` properties, `static` lambdas, `Lock`, etc.).

---

## Solution Layout

| Project | Purpose |
|---|---|
| `Stellar` | Core: interfaces, `ViewModelBase`, `ViewManager<T>`, `WeakCompositeDisposable`, extension methods, attributes |
| `Stellar.Maui` | MAUI platform: page/view/cell base classes, navigation extensions, scheduler, hot-reload |
| `Stellar.Maui.PopUp` | Mopups integration: `PopupPageBase<T>`, `ReactivePopupPage<T>` |
| `Stellar.Blazor` | Blazor platform: `ComponentBase<T>`, `LayoutComponentBase<T>`, `InjectableComponentBase<T>` |
| `Stellar.Avalonia` | Avalonia platform: `WindowBase<T>`, `UserControlBase<T>` |
| `Stellar.FluentValidation` | `FluentValidatorFor<T>` bridges FluentValidation → `IProvideValidation<T>` |
| `Stellar.DiskDataCache` | `DiskCache : IDataCache` — concurrent JSON disk cache with rate limiting |
| `Stellar.SourceGenerators` | Roslyn source generator: emits `AddRegisteredServicesFor{Assembly}()` from `[ServiceRegistration]` |
| `Stellar.UnitTests` | xUnit unit tests |

---

## Core Patterns — ALWAYS Follow These

### 1. ViewModel

```csharp
// REQUIRED: partial class + [ServiceRegistration]
[ServiceRegistration]                    // Transient by default
// [ServiceRegistration(Lifetime.Singleton)]
// [ServiceRegistration(Lifetime.Scoped)]
public partial class MyViewModel : ViewModelBase  // or ValidatingViewModelBase<MyViewModel>
{
    // Reactive properties use [Reactive] from ReactiveGenerator — class must be partial
    [Reactive]
    private string _myProperty;          // generates public MyProperty { get; set; }

    [Reactive]
    private ReactiveCommand<Unit, Unit> _myCommand;

    // Constructor injection — primary constructor syntax preferred
    public MyViewModel(IMyService service)
    {
        _service = service;
    }

    // One-time initialization (not reactive bindings)
    protected override void Initialize()
    {
        // Set initial values, seed data, etc.
    }

    // Reactive bindings — ALWAYS use WeakCompositeDisposable
    protected override void Bind(WeakCompositeDisposable disposables)
    {
        MyCommand =
            ReactiveCommand
                .Create(DefaultAction)    // or .CreateFromTask / .CreateFromObservable
                .DisposeWith(disposables);

        this.WhenAnyValue(static x => x.MyProperty)
            .Subscribe(value => { /* side effect */ })
            .DisposeWith(disposables);
    }
}
```

**Rules:**
- Class must be `partial` when using `[Reactive]`
- `[Reactive]` goes on **private backing fields** (camelCase with underscore prefix)
- All `IDisposable` results of `Subscribe`, `Bind`, `BindCommand`, `ReactiveCommand.Create` **must** be `.DisposeWith(disposables)` in `Bind()`
- Use `static` lambdas in `WhenAnyValue`, `OneWayBind`, `Bind`, `BindCommand` to avoid unintended captures
- `DefaultAction` (from `ViewModelBase`) is a pre-built `() => {}` — use it for commands with no logic

### 2. MAUI Views (Pages, ContentViews, Cells)

```csharp
[ServiceRegistration]
public class MyPage : ContentPageBase<MyViewModel>   // or ContentViewBase<T>, ViewCellBase<T>, etc.
{
    // Private field for each UI control
    private Label _myLabel;
    private Button _myButton;

    // Constructor: inject ViewModel, call InitializeStellarComponent
    public MyPage(MyViewModel viewModel)
    {
        this.InitializeStellarComponent(viewModel);
    }

    // Build the UI using C# Markup (CommunityToolkit.Maui.Markup)
    public override void SetupUserInterface()
    {
        Content =
            new Grid
            {
                ColumnDefinitions = Columns.Define([Star]),
                RowDefinitions = Rows.Define([Auto, Star]),
                Padding = 8,
                RowSpacing = 8,
                Children =
                {
                    new Label()
                        .Row(0).Column(0)
                        .Assign(out _myLabel),

                    new Button { Text = "Go" }
                        .Row(1).Column(0)
                        .Assign(out _myButton),
                },
            };
    }

    // Reactive bindings to ViewModel
    public override void Bind(WeakCompositeDisposable disposables)
    {
        this.OneWayBind(ViewModel, static vm => vm.MyProperty, static ui => ui._myLabel.Text)
            .DisposeWith(disposables);

        this.BindCommand(ViewModel, static vm => vm.MyCommand, static ui => ui._myButton, Observables.UnitDefault)
            .DisposeWith(disposables);
    }
}
```

**MAUI Base Classes:**
| Class | Base for |
|---|---|
| `ContentPageBase<TVM>` | Full-screen pages |
| `ContentViewBase<TVM>` | Reusable content views |
| `ContentViewBase<TVM, TData>` | Views with a data model that maps to VM |
| `ViewCellBase<TVM>` | ListView / CollectionView cells |
| `ViewCellBase<TVM, TData>` | Cells with data model mapping |
| `GridBase<TVM>` | Views based on Grid |
| `StackLayoutBase<TVM>` | Views based on StackLayout |
| `PopupPageBase<TVM>` | Mopups popup pages |
| `ShellBase<TVM>` | Shell-based app shells |
| `TabbedPageBase<TVM>` | TabbedPage |

### 3. Binding Methods

```csharp
// One-way: VM → UI
this.OneWayBind(ViewModel, static vm => vm.Prop, static ui => ui._control.Prop)
    .DisposeWith(disposables);

// One-way with converter
this.OneWayBind(ViewModel, static vm => vm.IsValid, static ui => ui._box.Color,
    static x => x ? Colors.Green : Colors.Red)
    .DisposeWith(disposables);

// Two-way: VM ↔ UI
this.Bind(ViewModel, static vm => vm.Text, static ui => ui._entry.Text)
    .DisposeWith(disposables);

// Command binding
this.BindCommand(ViewModel, static vm => vm.MyCommand, static ui => ui._button, Observables.UnitDefault)
    .DisposeWith(disposables);

// WhenAnyValue from view
this.WhenAnyValue(static x => x.ViewModel.SomeProperty)
    .Select(static x => $"Formatted: {x}")
    .BindTo(this, static ui => ui._label.Text)
    .DisposeWith(disposables);

// WhenAnyObservable — observe a command's output
this.WhenAnyObservable(static x => x.ViewModel.MyCommand)
    .Subscribe(result => { })
    .DisposeWith(disposables);
```

### 4. Navigation (MAUI)

```csharp
// Navigate to a page (resolves from DI)
someObservable
    .NavigateToPage<MyNextPage>(this)
    .DisposeWith(disposables);

// Navigate with typed parameter and query parameters
someObservable
    .NavigateToPage<int, MyNextPage>(
        this,
        queryParameters: static (value, dict) =>
        {
            dict.Add("ParameterValue", value);
        })
    .DisposeWith(disposables);

// Show popup (Mopups)
someObservable
    .NavigateToPopupPage<MyPopupPage>()
    .DisposeWith(disposables);

// Modal
someObservable
    .NavigateToModalPage<MyModalPage>(this)
    .DisposeWith(disposables);
```

**Receiving query parameters in ViewModel:**
```csharp
[QueryParameter]
public long ParameterValue
{
    get => _parameterValue;
    set => this.RaiseAndSetIfChanged(ref _parameterValue, value);
}
```

### 5. Lifecycle Events

```csharp
// In a View — all are IObservable<Unit>
this.IsAppearing
    .Subscribe(_ => RefreshData())
    .DisposeWith(disposables);

// In a ViewModel — implement ILifecycleEventAware
public partial class MyViewModel : ViewModelBase, ILifecycleEventAware
{
    public void OnLifecycleEvent(LifecycleEvent lifecycleEvent)
    {
        if (lifecycleEvent == LifecycleEvent.IsAppearing)
            LoadData();
    }
}

// LifecycleEvent values:
// Unknown, Initialized, Activated, Attached, IsAppearing, IsDisappearing, Detached, Deactivated, Disposed
```

### 6. Validation

```csharp
// Validator (using FluentValidation bridge)
public class MyViewModelValidator : FluentValidatorFor<MyViewModel>
{
    public MyViewModelValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2);
    }
}

// ViewModel
[ServiceRegistration]
public partial class MyViewModel : ValidatingViewModelBase<MyViewModel>
{
    [Reactive]
    private string _email;

    [Reactive]
    private string _name;

    public MyViewModel(MyViewModelValidator validator)
        : base(validator)
    {
    }

    protected override void Bind(WeakCompositeDisposable disposables)
    {
        this.RegisterValidation()    // auto-triggers on PropertyChanged
            .DisposeWith(disposables);
    }
}

// In View — bind IsValid and ValidationErrors
this.OneWayBind(ViewModel, static vm => vm.IsValid, static ui => ui._submitButton.IsEnabled)
    .DisposeWith(disposables);

// Monitor per-property validation
ViewModel.MonitorValidationInformationFor(static vm => vm.Email)
    .Subscribe(info => _emailError.Text = info.IsValid ? string.Empty : info.ErrorMessage)
    .DisposeWith(disposables);
```

### 7. Service Registration & DI

```csharp
// Attribute-driven registration (Transient by default)
[ServiceRegistration]
[ServiceRegistration(Lifetime.Singleton)]
[ServiceRegistration(Lifetime.Scoped)]
[ServiceRegistration(Lifetime.Transient, registerInterfaces: true)]  // also registers implemented interfaces

// In MauiProgram.cs — source-generated extension:
builder.Services.AddRegisteredServicesForMyAppAssemblyName();

// OR — runtime reflection (fallback):
builder.UseStellarComponents();  // scans assembly for [ServiceRegistration]
```

### 8. Disk Cache

```csharp
// Registration
services.AddSingleton<IDataCache>(sp =>
    new DiskCache(FileSystem.AppDataDirectory));

// Usage in ViewModel
var item = await _cache.RetrieveAsync<MyModel>(cacheKey: "my-key", groupKey: "group");
await _cache.StoreAsync(item, cacheKey: x => x.Id, groupKey: "group");
var all = await _cache.RetrieveManyAsync<MyModel>("group");
await _cache.RemoveAsync<MyModel>(cacheKey: "my-key");
await _cache.ClearCacheAsync("group");  // or null to clear default "Cache" folder
```

### 9. Avalonia Views

```csharp
[ServiceRegistration]
public partial class MyWindow : WindowBase<MyViewModel>
{
    public MyWindow(MyViewModel viewModel)
    {
        this.InitializeStellarComponent(viewModel);
    }

    public override void SetupUserInterface()
    {
        Content = new TextBlock()
            .Assign(out _textBlock);
    }

    public override void Bind(WeakCompositeDisposable disposables)
    {
        this.OneWayBind(ViewModel, static vm => vm.Text, static ui => ui._textBlock.Text)
            .DisposeWith(disposables);
    }
}
```

### 10. Blazor Components

```csharp
@inherits Stellar.Blazor.ComponentBase<MyViewModel>

// In code-behind (*.razor.cs):
[ServiceRegistration]
public partial class MyComponent : ComponentBase<MyViewModel>
{
    public override void SetupUserInterface() { }

    public override void Bind(WeakCompositeDisposable disposables)
    {
        this.WhenAnyValue(static x => x.ViewModel.Title)
            .Subscribe(_ => StateHasChanged())
            .DisposeWith(disposables);
    }
}
```

---

## Memory Management Rules

- **Always** `.DisposeWith(disposables)` for every `Subscribe`, command creation, and binding inside `Bind()`
- `WeakCompositeDisposable` is backed by `ConditionalWeakTable` — lifetime is tied to the owning object
- Use `WeakReference<T>` when lambdas capture ViewModel instances in `ValidatingViewModelBase`
- Use `static` lambdas (e.g., `static x => x.Prop`) to prevent unintended heap allocations / closures
- `Volatile.Read/Write` is used for thread-safe bool flags — prefer this over `lock` for simple flags

---

## Rx / Observable Patterns

```csharp
// Available IObservable extension methods (Stellar namespace):
.SelectUnit()                  // → IObservable<Unit>
.IsNotNull()                   // filter nulls
.IsNull()                      // filter non-nulls
.IsNotNullOrEmpty()            // for strings
.WhereIsTrue()                 // for bool streams
.WhereIsFalse()
.WhereHasValue()               // for Nullable<T>
.GetValueOrDefault(default)
.ThrottleFirst(duration, scheduler)   // multi-tap protection

// Useful Rx patterns in bindings:
Observable.Merge(obs1, obs2)           // combine multiple triggers
.Select(static _ => 0)                 // map to value
.Do(async _ => { ... })               // async side-effects
```

---

## Code Style Requirements

- Nullable reference types: **enabled** — always handle nullability
- `[Reactive]` properties: **always private backing field** + `partial class`
- Bindings use **static lambdas** wherever possible: `static vm => vm.Prop`
- UI controls are **private fields** captured with `.Assign(out _field)` in `SetupUserInterface()`
- No `BindingContext` manipulation in code-behind — use Stellar binding APIs
- No logic in constructors beyond calling `InitializeStellarComponent(viewModel)` for views
- StyleCop + Roslynator analyzers are active — follow existing code style

---

## Common Mistakes to Avoid

1. **Forgetting `partial`** on ViewModels using `[Reactive]` — source generator requires it
2. **Not calling `DisposeWith`** on subscriptions — causes memory leaks
3. **Using non-static lambdas** in bindings — creates closures that capture `this`
4. **Putting reactive bindings in `Initialize()`** — they belong in `Bind()`
5. **Navigating outside an Observable pipeline** — always use `NavigateToPage`/`NavigateToPopupPage` extensions
6. **Creating commands outside `Bind()`** — commands need the `disposables` container
7. **Using XAML** for UI — this codebase uses **C# Markup only** for MAUI
8. **Registering services manually in startup** when `[ServiceRegistration]` + source generator handles it

---

## Scheduler Conventions

- `RxApp.MainThreadScheduler` — UI thread work (updated to MAUI dispatcher by `MauiScheduler`)
- `RxApp.TaskpoolScheduler` — background work (set to `Schedulers.ShortTermThreadPoolScheduler`)
- `Schedulers.ShortTermThreadPoolScheduler` — for page/navigation creation off main thread
- Always `ObserveOn(RxApp.MainThreadScheduler)` before updating UI properties

---

## Project-Specific Utilities

- `Observables.UnitDefault` — pre-created `IObservable<Unit>` emitting `Unit.Default` immediately
- `AttributeCache` — thread-safe, concurrent-dictionary-backed attribute lookup cache; use instead of raw `Attribute.GetCustomAttribute`
- `[PreCacheAttribute]` — marks a type for eager instantiation at startup (warm-up caching)
- `[QueryParameter]` — marks a ViewModel property to receive navigation query parameters

---

## Testing

Tests use **xUnit**. Test ViewModels by instantiating them directly and calling `SetupViewModel()` then `Register()`. No mock framework is currently used in the test project.

```csharp
var vm = new MyViewModel(new MyService());
vm.SetupViewModel();   // calls Initialize() then Register() -> Bind()
Assert.True(vm.Initialized);
Assert.True(vm.BindingsRegistered);
```
