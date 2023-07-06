namespace Stellar.ViewModel;

public abstract class ViewModelBase : ReactiveObject, IViewModel, IDisposable
{
    protected static readonly Action DefaultAction = () => { };

    private readonly object _vmLock = new();

    private readonly CompositeDisposable _viewModelBindings = new();

    private bool _bindingsRegistered;

    private bool _initialized;

    public bool Maintain { get; set; }

    public bool IsDisposed { get; private set; }

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

    public void SetupViewModel()
    {
        InitializeInternal();

        Register();
    }

    public void Register()
    {
        lock (_vmLock)
        {
            if (_bindingsRegistered)
            {
                return;
            }

            _viewModelBindings.Clear();
            Bind(_viewModelBindings);

            _bindingsRegistered = true;
        }
    }

    public void Unregister()
    {
        lock (_vmLock)
        {
            if (Maintain || !_bindingsRegistered)
            {
                return;
            }

            _viewModelBindings.Clear();

            _bindingsRegistered = false;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Initialize()
    {
    }

    protected abstract void Bind(CompositeDisposable disposables);

    protected virtual void Dispose(bool disposing)
    {
        if (IsDisposed)
        {
            return;
        }

        IsDisposed = true;

        if (disposing)
        {
            _viewModelBindings.Dispose();
        }
    }

    private void InitializeInternal()
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
    }
}
