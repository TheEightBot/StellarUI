# Create a new Avalonia View

Create a StellarUI Avalonia view (UserControl or Window) following the codebase patterns.

## Instructions

### Avalonia UserControl (ContentControlBase)

Avalonia views use AXAML markup (not pure C# like MAUI). The code-behind follows the IStellarView contract.

**AXAML file** (`${Name}View.axaml`):
```xml
<reactive:ReactiveUserControl
    x:Class="${Namespace}.UserInterface.Views.${Name}View"
    x:TypeArguments="vm:${Name}ViewModel"
    xmlns="https://github.com/avaloniaui"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:reactive="http://reactiveui.net"
    xmlns:vm="clr-namespace:${Namespace}.ViewModels">

    <!-- UI defined in AXAML -->
    <Grid RowDefinitions="Auto,*" ColumnDefinitions="*">
        <TextBlock Grid.Row="0" x:Name="TitleTextBlock" />
        <ListBox Grid.Row="1" x:Name="ItemsListBox" />
    </Grid>

</reactive:ReactiveUserControl>
```

**Code-behind** (`${Name}View.axaml.cs`):
```csharp
using Avalonia.ReactiveUI;
using Stellar.Avalonia;

namespace ${Namespace}.UserInterface.Views;

[ServiceRegistration]
public partial class ${Name}View : ContentControlBase<${Name}ViewModel>
{
    public ${Name}View()
    {
        InitializeComponent();
        this.InitializeStellarComponent();
    }

    public override void SetupUserInterface()
    {
        // AXAML-defined views typically don't need this override
        // Use it for programmatic post-AXAML adjustments only
    }

    public override void Bind(WeakCompositeDisposable disposables)
    {
        this.OneWayBind(ViewModel, static vm => vm.Title, static ui => ui.TitleTextBlock.Text)
            .DisposeWith(disposables);

        this.OneWayBind(ViewModel, static vm => vm.Items, static ui => ui.ItemsListBox.ItemsSource)
            .DisposeWith(disposables);
    }
}
```

### Avalonia Window (WindowBase)

**AXAML file** (`${Name}Window.axaml`):
```xml
<reactive:ReactiveWindow
    x:Class="${Namespace}.UserInterface.Windows.${Name}Window"
    x:TypeArguments="vm:${Name}ViewModel"
    xmlns="https://github.com/avaloniaui"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:reactive="http://reactiveui.net"
    xmlns:vm="clr-namespace:${Namespace}.ViewModels"
    Title="${Name}"
    Width="800"
    Height="600">

    <Grid>
        <!-- Window content -->
    </Grid>

</reactive:ReactiveWindow>
```

**Code-behind** (`${Name}Window.axaml.cs`):
```csharp
using Avalonia.ReactiveUI;
using Stellar.Avalonia;

namespace ${Namespace}.UserInterface.Windows;

[ServiceRegistration]
public partial class ${Name}Window : WindowBase<${Name}ViewModel>
{
    public ${Name}Window()
    {
        InitializeComponent();
        this.InitializeStellarComponent();
    }

    public override void SetupUserInterface()
    {
        // Post-AXAML initialization if needed
    }

    public override void Bind(WeakCompositeDisposable disposables)
    {
        // Reactive bindings using WhenActivated (ReactiveUI Avalonia pattern)
        this.WhenActivated(d =>
        {
            this.OneWayBind(ViewModel, static vm => vm.Title, static ui => ui.TitleTextBlock.Text)
                .DisposeWith(d);
        });
    }
}
```

## Key Rules
- Avalonia views inherit from `ContentControlBase<TVM>` or `WindowBase<TVM>` from `Stellar.Avalonia`
- Call `this.InitializeStellarComponent()` (no args) in constructor, after `InitializeComponent()`
- AXAML `x:Name` attributes create typed fields — use them directly in `Bind()`
- Still use static lambdas in all binding expressions
- Always `.DisposeWith(disposables)` in `Bind()`
- ViewModels are shared with MAUI — same `ViewModelBase` pattern applies
