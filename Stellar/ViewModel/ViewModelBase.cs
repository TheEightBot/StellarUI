namespace Stellar.ViewModel;

using Stellar.Extensions;

#pragma warning disable CA1001
public abstract class ViewModelBase : ReactiveObject, IViewModel
#pragma warning restore CA1001
{
    protected static readonly Action DefaultAction = () => { };

    private readonly Lock _vmLock = new();
    private readonly WeakCompositeDisposable _viewModelBindings;

    // Nullable bool rather than Lazy<bool>: a view model is created per view, and the
    // Lazy plus its capturing closure were two heap allocations per instance for a value
    // that is only read when bindings are unregistered. AttributeCache already memoises
    // the reflection per type, so recomputing on a race is cheap.
    private bool? _shouldMaintain;

    private bool _bindingsRegistered;
    private bool _initialized;
    private bool _isDisposed;

    protected ViewModelBase()
    {
        _viewModelBindings = new(this);
    }

    private bool ShouldMaintain =>
        _shouldMaintain ??=
            AttributeCache.GetAttribute<ServiceRegistrationAttribute>(GetType())
                is { ServiceRegistrationType: Lifetime.Scoped or Lifetime.Singleton };

    public bool Maintain { get; set; }

    public bool IsDisposed => Volatile.Read(ref _isDisposed);

    public bool Initialized
    {
        // Using volatile reads instead of full locks for better performance
        get => Volatile.Read(ref _initialized);
        private set => _initialized = value;
    }

    public bool BindingsRegistered
    {
        // Using volatile reads instead of full locks for better performance
        get => Volatile.Read(ref _bindingsRegistered);
        private set => _bindingsRegistered = value;
    }

    public void SetupViewModel()
    {
        InitializeInternal();
        Register();
    }

    public void Register()
    {
        if (IsDisposed)
        {
            return;
        }

        lock (_vmLock)
        {
            if (_bindingsRegistered || IsDisposed)
            {
                return;
            }

            _viewModelBindings.Clear();
            Bind(_viewModelBindings);

            BindingsRegistered = true;
        }
    }

    public void Unregister()
    {
        if (IsDisposed)
        {
            return;
        }

        lock (_vmLock)
        {
            if (Maintain || !_bindingsRegistered || IsDisposed)
            {
                return;
            }

            _viewModelBindings.Clear();
            BindingsRegistered = false;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (Volatile.Read(ref _isDisposed))
        {
            return;
        }

        if (disposing)
        {
            _viewModelBindings.Dispose();
        }

        Volatile.Write(ref _isDisposed, true);
    }

    protected virtual void Initialize()
    {
    }

    protected abstract void Bind(WeakCompositeDisposable disposables);

    private void InitializeInternal()
    {
        if (Initialized)
        {
            return;
        }

        lock (_vmLock)
        {
            if (!_initialized)
            {
                // Use cached attribute lookup
                Maintain = ShouldMaintain;
                Initialize();
                Initialized = true;
            }
        }
    }
}
