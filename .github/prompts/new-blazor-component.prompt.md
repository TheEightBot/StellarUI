# Create a new Blazor Component

Create a StellarUI Blazor component following the codebase patterns.

## Instructions

### Standard Blazor Component (ReactiveInjectableComponentBase)

Blazor components in StellarUI use `ReactiveInjectableComponentBase<TViewModel>` from `Stellar.Blazor`.

**Razor file** (`${Name}Component.razor`):
```razor
@inherits Stellar.Blazor.ReactiveInjectableComponentBase<${Namespace}.ViewModels.${Name}ViewModel>
@using ${Namespace}.ViewModels

@if (ViewModel is not null)
{
    <div class="container">
        <h1>@ViewModel.Title</h1>

        <ul>
        @foreach (var item in ViewModel.Items)
        {
            <li>@item.Name</li>
        }
        </ul>

        <button @onclick="() => ViewModel.LoadCommand.Execute(Unit.Default).Subscribe()">
            Load
        </button>
    </div>
}
```

**Code-behind** (`${Name}Component.razor.cs`):
```csharp
namespace ${Namespace}.UserInterface.Components;

[ServiceRegistration]
public partial class ${Name}Component : ReactiveInjectableComponentBase<${Name}ViewModel>
{
    public override void SetupUserInterface()
    {
        // Not typically used for Blazor — markup is in .razor file
    }

    public override void Bind(WeakCompositeDisposable disposables)
    {
        // Subscribe to ViewModel observables; call StateHasChanged to trigger re-render
        ViewModel!
            .WhenAnyValue(static vm => vm.Items)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => StateHasChanged())
            .DisposeWith(disposables);
    }
}
```

### Blazor Page Component

```razor
@page "/my-route"
@inherits Stellar.Blazor.ReactiveInjectableComponentBase<${Namespace}.ViewModels.${Name}ViewModel>
@using ${Namespace}.ViewModels

@if (ViewModel is not null)
{
    <PageTitle>${Name}</PageTitle>

    <div>
        @if (ViewModel.IsLoading)
        {
            <p>Loading...</p>
        }
        else
        {
            <p>@ViewModel.Content</p>
        }
    </div>
}
```

### Notes on Reactivity

Blazor doesn't have automatic property-change UI refresh from ReactiveUI.
You must manually trigger `StateHasChanged()` when ViewModel data changes:

```csharp
public override void Bind(WeakCompositeDisposable disposables)
{
    // Re-render on any property change that affects UI
    ViewModel!
        .WhenAnyValue(
            static vm => vm.IsLoading,
            static vm => vm.Title,
            static vm => vm.Items)
        .ObserveOn(RxSchedulers.MainThreadScheduler)
        .Subscribe(_ => StateHasChanged())
        .DisposeWith(disposables);
}
```

## Key Rules
- Inherit from `ReactiveInjectableComponentBase<TViewModel>` (from `Stellar.Blazor`)
- ViewModel is injected via DI — must be registered with `[ServiceRegistration]`
- Always null-check `ViewModel` in Razor: `@if (ViewModel is not null)`
- Manually call `StateHasChanged()` when ViewModel changes need to re-render UI
- Blazor lifecycle: `OnInitializedAsync` maps to `Initialize()`, bindings set up in `Bind()`
- Disposing handled by `WeakCompositeDisposable` — subscribe in `Bind()` with `.DisposeWith(disposables)`
