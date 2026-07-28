# Create a new Service

Create a StellarUI service following the dependency injection patterns.

## Instructions

Services implement an interface and are registered via `[ServiceRegistration]`.

### Interface

```csharp
namespace ${Namespace}.Services;

public interface I${Name}Service
{
    // Define service contract
    Task<${ResultType}> GetAsync(string id, CancellationToken cancellationToken = default);
    Task SaveAsync(${ModelType} model, CancellationToken cancellationToken = default);
}
```

### Implementation

```csharp
namespace ${Namespace}.Services;

// Use the appropriate lifetime:
// [ServiceRegistration] — Transient (new instance each request)
// [ServiceRegistration(Lifetime.Singleton)] — single instance for app lifetime
// [ServiceRegistration(Lifetime.Scoped)] — one per scope
// Add registerInterfaces: true to also register the interface
[ServiceRegistration(Lifetime.Singleton, registerInterfaces: true)]
public class ${Name}Service : I${Name}Service
{
    // Inject dependencies via primary constructor
    private readonly IDataCache _cache;
    private readonly ILogger<${Name}Service> _logger;

    public ${Name}Service(IDataCache cache, ILogger<${Name}Service> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<${ResultType}> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        // Try cache first
        var cached = await _cache.RetrieveAsync<${ResultType}>(cacheKey: id, groupKey: "${Name}");
        if (cached is not null)
            return cached;

        // Fetch from source...
        var result = default(${ResultType});

        // Cache it
        await _cache.StoreAsync(result, cacheKey: id, groupKey: "${Name}");

        return result!;
    }

    public async Task SaveAsync(${ModelType} model, CancellationToken cancellationToken = default)
    {
        await _cache.StoreAsync(model, cacheKey: model.Id, groupKey: "${Name}");
    }
}
```

### Injecting into a ViewModel

```csharp
[ServiceRegistration]
public partial class MyViewModel : ViewModelBase
{
    private readonly I${Name}Service _service;

    public MyViewModel(I${Name}Service service)
    {
        _service = service;
    }

    protected override void Bind(WeakCompositeDisposable disposables)
    {
        this.WhenAnyValue(static x => x.SomeProperty)
            .SelectMany(x => _service.GetAsync(x).ToObservable())
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(result => Items = result)
            .DisposeWith(disposables);
    }
}
```

## Key Rules
- `[ServiceRegistration]` goes on the **implementation** class, not the interface
- Use `registerInterfaces: true` to register interface → implementation mapping
- Services are auto-discovered by the source generator or `UseStellarComponents()`
- For `Singleton` services with disposable resources, implement `IDisposable`
- Use `Lifetime.Singleton` for stateful, shared, or expensive-to-create services
- Use `Lifetime.Transient` (default) for stateless, lightweight services
