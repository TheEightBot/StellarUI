# Create a MAUI ViewCell

Create a StellarUI MAUI ListView/CollectionView cell following the codebase patterns.

## Instructions

Use `ViewCellBase<TViewModel>` for cells where the data comes from a typed ViewModel,
or `ViewCellBase<TViewModel, TDataModel>` when the BindingContext is a plain data model.

### Simple ViewCell (ViewModel-driven)

```csharp
namespace ${Namespace}.UserInterface.Cells;

public class ${Name}Cell : ViewCellBase<${ItemType}>
{
    // private Label _name;
    // private Label _detail;

    public ${Name}Cell()
    {
        this.InitializeStellarComponent();  // no ViewModel arg — set via BindingContext
    }

    public override void SetupUserInterface()
    {
        View =
            new Grid
            {
                ColumnDefinitions = Columns.Define([Star, Auto]),
                RowDefinitions = Rows.Define([Auto]),
                Padding = new Thickness(12, 8),
                ColumnSpacing = 8,
                Children =
                {
                    new Label()
                        .Row(0).Column(0)
                        .Assign(out _name),

                    new Label()
                        .Row(0).Column(1)
                        .Assign(out _detail),
                },
            };
    }

    public override void Bind(WeakCompositeDisposable disposables)
    {
        this.OneWayBind(ViewModel, static vm => vm.Name, static ui => ui._name.Text)
            .DisposeWith(disposables);

        this.OneWayBind(ViewModel, static vm => vm.Detail, static ui => ui._detail.Text)
            .DisposeWith(disposables);
    }
}
```

### Data-Mapped ViewCell (plain model → ViewModel)

```csharp
namespace ${Namespace}.UserInterface.Cells;

public class ${Name}Cell : ViewCellBase<${Name}CellViewModel, ${DataModel}>
{
    // private Label _name;

    public ${Name}Cell()
    {
        this.InitializeStellarComponent();
    }

    protected override void MapDataModelToViewModel(${Name}CellViewModel viewModel, ${DataModel} dataModel)
    {
        viewModel.Name = dataModel.DisplayName;
        // Map other properties as needed
    }

    public override void SetupUserInterface()
    {
        View =
            new Label()
                .Assign(out _name);
    }

    public override void Bind(WeakCompositeDisposable disposables)
    {
        this.OneWayBind(ViewModel, static vm => vm.Name, static ui => ui._name.Text)
            .DisposeWith(disposables);
    }
}
```

### Using in a ListView

```csharp
// In SetupUserInterface():
new ListView
{
    ItemTemplate = new DataTemplate(typeof(${Name}Cell)),
    HasUnevenRows = true,
}
    .Assign(out _listView)

// In Bind():
this.OneWayBind(ViewModel, static vm => vm.Items, static ui => ui._listView.ItemsSource)
    .DisposeWith(disposables);

// Handle item tapped and navigate:
_listView
    .ItemTapped<${ItemType}>(true)    // true = deselect after tap
    .Select(static x => x.Id)
    .NavigateToPage<int, ${DetailPage}>(
        this,
        queryParameters: static (id, dict) => dict.Add("ItemId", id))
    .DisposeWith(disposables);
```

## Key Rules
- `InitializeStellarComponent()` with **no arguments** for cells — ViewModel set via BindingContext
- Assign cell root view to the `View` property in `SetupUserInterface()`
- Always use `RecycleElement` caching strategy (default in Stellar's `ActivatableListView`)
- Static lambdas in all bindings
- Everything `.DisposeWith(disposables)` in `Bind()`
