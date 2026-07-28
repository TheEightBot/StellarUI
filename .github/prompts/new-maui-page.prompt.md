# Create a new MAUI Page

Create a new StellarUI MAUI page following the exact patterns used in this codebase.

## Instructions

Given a page name and ViewModel name, generate:
1. The ViewModel class
2. The Page class

### ViewModel Template

```csharp
using Stellar.MauiSample.Services;  // adjust namespace

namespace ${Namespace}.ViewModels;

[ServiceRegistration]
public partial class ${Name}ViewModel : ViewModelBase
{
    // Add injected services here via primary constructor
    // public ${Name}ViewModel(IMyService service) { }

    // Reactive properties
    // [Reactive]
    // private string _myProperty;

    // Reactive commands
    // [Reactive]
    // private ReactiveCommand<Unit, Unit> _myCommand;

    protected override void Initialize()
    {
        // One-time setup: initial values, seed data
    }

    protected override void Bind(WeakCompositeDisposable disposables)
    {
        // Reactive bindings and commands
        // MyCommand = ReactiveCommand.Create(DefaultAction).DisposeWith(disposables);
    }
}
```

### Page Template

```csharp
namespace ${Namespace}.UserInterface.Pages;

[ServiceRegistration]
public class ${Name}Page : ContentPageBase<${Name}ViewModel>
{
    // Private field for every UI control
    // private Label _myLabel;
    // private Button _myButton;

    public ${Name}Page(${Name}ViewModel viewModel)
    {
        this.InitializeStellarComponent(viewModel);
    }

    public override void SetupUserInterface()
    {
        Content =
            new Grid
            {
                ColumnDefinitions = Columns.Define([Star]),
                RowDefinitions = Rows.Define([Auto, Star]),
                Padding = 16,
                RowSpacing = 8,
                Children =
                {
                    // Add controls here, use .Row(n).Column(n).Assign(out _field)
                },
            };
    }

    public override void Bind(WeakCompositeDisposable disposables)
    {
        // this.OneWayBind(ViewModel, static vm => vm.Prop, static ui => ui._control.Prop)
        //     .DisposeWith(disposables);

        // this.BindCommand(ViewModel, static vm => vm.MyCommand, static ui => ui._button, Observables.UnitDefault)
        //     .DisposeWith(disposables);
    }
}
```

## Key Rules
- `partial` on ViewModel is **required** for `[Reactive]` to work
- All subscriptions and commands **must** be `.DisposeWith(disposables)` in `Bind()`
- Use **static lambdas** in all binding expressions
- UI is built in **C# code** — no XAML
- `InitializeStellarComponent(viewModel)` is the **only** call in the constructor
