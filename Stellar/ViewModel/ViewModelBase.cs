namespace Stellar.ViewModel;

using Stellar.Extensions;

#pragma warning disable CA1001
public abstract class ViewModelBase : ReactiveObject, IViewModel
#pragma warning restore CA1001
{
    protected static readonly Action DefaultAction = () => { };

    private readonly Lock _vmLock = new();
    private readonly WeakCompositeDisposable _viewModelBindings;
    private readonly Lazy<bool> _shouldMaintain;

    private bool _bindingsRegistered;
    private bool _initialized;
    private bool _isDisposed;

    protected ViewModelBase()
    {
        _viewModelBindings = new(this);

        // Cache attribute lookup using lazy initialization with the fast AttributeCache
        _shouldMaintain = new Lazy<bool>(() =>
        {
            var sra = AttributeCache.GetAttribute<ServiceRegistrationAttribute>(this.GetType());
            if (sra != null)
            {
                return sra.ServiceRegistrationType is Lifetime.Scoped or Lifetime.Singleton;
            }

            return false;
        });
    }

    public bool Maintain { get; set; }

    public bool IsDisposed => _isDisposed;

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
            if (_bindingsRegistered)
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
            if (Maintain || !_bindingsRegistered)
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
        if (_isDisposed)
        {
            return;
        }

        if (disposing)
        {
            _viewModelBindings.Dispose();
        }

        _isDisposed = true;
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
                Maintain = _shouldMaintain.Value;
                Initialize();
                Initialized = true;
            }
        }
    }
}
