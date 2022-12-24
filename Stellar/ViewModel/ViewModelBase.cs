namespace Stellar.ViewModel;

public abstract class ViewModelBase : ReactiveObject, IViewModel, IDisposable
{
    protected static Action DefaultAction = () => { };

    private readonly object _vmLock = new();

    private bool _initialized;

    protected bool _bindingsRegistered;

    private bool _isDisposed;

    public bool MaintainBindings { get; set; }

    protected CompositeDisposable ViewModelBindings { get; } = new();

    public void SetupViewModel()
    {
        lock (_vmLock)
        {
            if (!_initialized)
            {
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
        }
    }

    public void UnregisterBindings()
    {
        lock (_vmLock)
        {
            if (MaintainBindings || !_bindingsRegistered)
            {
                return;
            }

            ViewModelBindings?.Clear();
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
