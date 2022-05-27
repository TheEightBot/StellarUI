namespace EightBot.Stellar.ViewModel;

public abstract class ViewModelBase : ReactiveObject, IViewModel, IDisposable
{
    protected static Action DefaultAction = new Action(() => { });

    private readonly object _bindingLock = new object();

    private readonly CompositeDisposable _viewModelBindings = new CompositeDisposable();

    protected CompositeDisposable ViewModelBindings => _viewModelBindings;

    protected bool _isLoading, _bindingsRegistered, _maintainBindings;
    private bool _disposedValue;

    public bool MaintainBindings { get; set; }

    protected ViewModelBase()
        : this(true)
    {
    }

    protected ViewModelBase(bool registerObservables = true)
    {
        InitializeInternal(registerObservables);
    }

    private void InitializeInternal(bool registerObservables)
    {
        Initialize();

        if (registerObservables)
        {
            RegisterBindings();
        }
    }

    protected virtual void Initialize()
    {
    }

    protected abstract void RegisterObservables();

    public void RegisterBindings()
    {
        lock (_bindingLock)
        {
            if (_bindingsRegistered)
            {
                return;
            }

            Volatile.Write(ref _bindingsRegistered, true);

            RegisterObservables();
        }
    }

    public void UnregisterBindings()
    {
        lock (_bindingLock)
        {
            if (MaintainBindings || !_bindingsRegistered)
            {
                return;
            }

            Volatile.Write(ref _bindingsRegistered, false);

            _viewModelBindings?.Clear();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _viewModelBindings?.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}