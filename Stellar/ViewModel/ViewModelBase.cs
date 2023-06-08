namespace Stellar.ViewModel;

public abstract class ViewModelBase : ReactiveObject, IViewModel, IDisposable
{
    protected static readonly Action DefaultAction = () => { };

    private readonly object _vmLock = new();

    private bool _bindingsRegistered;

    protected CompositeDisposable ViewModelBindings { get; } = new();

    private bool _initialized;

    private bool _isDisposed;

    public bool Maintain { get; set; }

    public bool Initialized
    {
        get
        {
            lock (_vmLock)
            {
                return _initialized;
            }
        }
    }

    public bool BindingsRegistered
    {
        get
        {
            lock (_vmLock)
            {
                return _bindingsRegistered;
            }
        }
    }

    public bool IsDisposed => _isDisposed;

    public void SetupViewModel()
    {
        lock (_vmLock)
        {
            if (!_initialized)
            {
                if (Attribute.GetCustomAttribute(this.GetType(), typeof(ServiceRegistrationAttribute)) is ServiceRegistrationAttribute sra)
                {
                    switch (sra.ServiceRegistrationType)
                    {
                        case Lifetime.Scoped:
                        case Lifetime.Singleton:
                            Maintain = true;
                            break;
                    }
                }

                Initialize();
                _initialized = true;
            }
        }

        RegisterBindings();
    }

    protected virtual void Initialize()
    {
    }

    protected abstract void RegisterObservables();

    public void RegisterBindings()
    {
        lock (_vmLock)
        {
            if (_bindingsRegistered)
            {
                return;
            }

            RegisterObservables();

            _bindingsRegistered = true;
        }
    }

    public void UnregisterBindings()
    {
        lock (_vmLock)
        {
            if (Maintain || !_bindingsRegistered)
            {
                return;
            }

            ViewModelBindings?.Clear();

            _bindingsRegistered = false;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        if (disposing)
        {
            ViewModelBindings?.Dispose();
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
