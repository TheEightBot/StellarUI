# Create a new MAUI ContentView

Create a reusable StellarUI MAUI content view (component) following the codebase patterns.

## Instructions

Use this for reusable UI components that are embedded within pages.
For views that need to display a plain data model (not a ViewModel), use the `TViewModel, TDataModel` variant.

### Standard ContentView (ViewModel-driven)

```csharp
namespace ${Namespace}.UserInterface.Views;

[ServiceRegistration]
public class ${Name}View : ContentViewBase<${Name}ViewModel>
{
    // private Label _myLabel;

    public ${Name}View(${Name}ViewModel viewModel)
    {
        this.InitializeStellarComponent(viewModel);
    }

    public override void SetupUserInterface()
    {
        Content =
            new VerticalStackLayout
            {
                Padding = 8,
                Spacing = 4,
                Children =
                {
                    // new Label().Assign(out _myLabel),
                },
            };
    }

    public override void Bind(WeakCompositeDisposable disposables)
    {
        // this.OneWayBind(ViewModel, static vm => vm.Prop, static ui => ui._myLabel.Text)
        //     .DisposeWith(disposables);
    }
}
```

### Data-Mapped ContentView (plain model → ViewModel mapping)

Use when the view's `BindingContext` is a plain data model (e.g., set by a parent ListView/CollectionView).

```csharp
namespace ${Namespace}.UserInterface.Views;

[ServiceRegistration]
public class ${Name}View : ContentViewBase<${Name}ViewModel, ${DataModel}>
{
    // private Label _myLabel;

    public ${Name}View(${Name}ViewModel viewModel)
    {
        this.InitializeStellarComponent(viewModel);
    }

    public override void SetupUserInterface()
    {
        Content =
            new Label()
                .Assign(out _myLabel);
    }

    // Map incoming data model to ViewModel properties
    protected override void MapDataModelToViewModel(${Name}ViewModel viewModel, ${DataModel} dataModel)
    {
        viewModel.SomeProperty = dataModel.SomeValue;
    }

    public override void Bind(WeakCompositeDisposable disposables)
    {
        this.OneWayBind(ViewModel, static vm => vm.SomeProperty, static ui => ui._myLabel.Text)
            .DisposeWith(disposables);
    }
}
```

## Key Rules
- Call `this.InitializeStellarComponent(viewModel)` in constructor
- All controls are **private fields** captured via `.Assign(out _field)`
- Bindings use **static lambdas** and always `.DisposeWith(disposables)`
- No XAML — C# code only
