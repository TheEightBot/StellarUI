# Create a new ViewModel (standalone)

Create a standalone StellarUI ViewModel with common patterns.

## Instructions

### Basic ViewModel

```csharp
namespace ${Namespace}.ViewModels;

[ServiceRegistration]
public partial class ${Name}ViewModel : ViewModelBase
{
    // Reactive properties — backing fields only, [Reactive] generates the public property
    [Reactive]
    private string _title = string.Empty;

    [Reactive]
    private bool _isLoading;

    [Reactive]
    private ObservableCollection<${ItemType}> _items = [];

    // Commands declared as reactive properties
    [Reactive]
    private ReactiveCommand<Unit, Unit> _loadCommand;

    [Reactive]
    private ReactiveCommand<${ItemType}, Unit> _selectCommand;

    // Service injection via constructor
    private readonly I${Name}Service _service;

    public ${Name}ViewModel(I${Name}Service service)
    {
        _service = service;
    }

    protected override void Initialize()
    {
        // One-time setup — runs before Bind()
        // Use for: initial property values, synchronous setup
    }

    protected override void Bind(WeakCompositeDisposable disposables)
    {
        // Create commands
        LoadCommand = ReactiveCommand
            .CreateFromTask(LoadAsync)
            .DisposeWith(disposables);

        SelectCommand = ReactiveCommand
            .Create<${ItemType}>(HandleSelection)
            .DisposeWith(disposables);

        // Handle command exceptions
        LoadCommand.ThrownExceptions
            .Subscribe(ex => Debug.WriteLine($"Load error: {ex.Message}"))
            .DisposeWith(disposables);

        // React to property changes
        this.WhenAnyValue(static x => x.Title)
            .Where(static x => !string.IsNullOrEmpty(x))
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(title => { /* respond to title change */ })
            .DisposeWith(disposables);

        // Auto-load on initialization
        this.WhenAnyValue(static x => x.IsInitialized)
            .Where(static x => x)
            .Take(1)
            .Select(static _ => Unit.Default)
            .InvokeCommand(this, static x => x.LoadCommand)
            .DisposeWith(disposables);
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        try
        {
            var results = await _service.GetAllAsync(cancellationToken);
            Items = new ObservableCollection<${ItemType}>(results);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void HandleSelection(${ItemType} item)
    {
        // Handle item selection
    }
}
```

### ViewModel with Query Parameters (navigation target)

```csharp
[ServiceRegistration]
public partial class ${Name}ViewModel : ViewModelBase
{
    // Receives navigation query parameters
    [QueryParameter]
    [Reactive]
    private int _itemId;

    [Reactive]
    private ${ItemType} _item;

    protected override void Bind(WeakCompositeDisposable disposables)
    {
        // React when ItemId is set (after navigation)
        this.WhenAnyValue(static x => x.ItemId)
            .Where(static id => id > 0)
            .SelectMany(id => LoadItemAsync(id).ToObservable())
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(item => Item = item)
            .DisposeWith(disposables);
    }

    private async Task<${ItemType}> LoadItemAsync(int id)
    {
        // Load item from service
        return default!;
    }
}
```

### SelectionViewModel (built-in for selection lists)

```csharp
// Use the built-in SelectionViewModel<TKey> for selection scenarios:
// It provides: Key, DisplayValue, Selected, ToggleSelected command

public partial class ${Name}SelectionItem : SelectionViewModel<${KeyType}>
{
    // Already has: TKey Key, string DisplayValue, bool Selected, ICommand ToggleSelected

    [Reactive]
    private string _subtitle;  // Add additional properties as needed
}
```

## Key Rules
- Class **must** be `partial` — required for `[Reactive]` source generator
- `[Reactive]` goes on **private backing fields** (`_camelCase`) — generates public property
- `Bind()` receives `WeakCompositeDisposable` — **every** subscription `.DisposeWith(disposables)`
- `Initialize()` for synchronous setup; `Bind()` for reactive subscriptions
- Use `WeakReference<T>` in long-lived lambdas that capture `this`
- Avoid capturing `this` in Observable pipelines; prefer `static` lambdas where possible
- Return observables **don't** need unsubscribing if using `DisposeWith(disposables)`
