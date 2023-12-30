namespace Stellar.ViewModel;

#pragma warning disable CA1001
public abstract class ViewModelBase : ReactiveObject, IViewModel
#pragma warning restore CA1001
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

    ~ViewModelBase()
    {
        System.Console.WriteLine("GCing view model base");
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

    protected virtual void Initialize()
    {
    }

    protected abstract void Bind(CompositeDisposable disposables);

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
