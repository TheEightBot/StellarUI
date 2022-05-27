namespace EightBot.Stellar.Maui.Pages;

public abstract class ContentPageBase<TViewModel> : ReactiveContentPage<TViewModel>, IStellarPage<TViewModel>
    where TViewModel : class
{
    private readonly ViewManager _viewManager = new ViewManager();

    private bool _disposedValue;

    public IObservable<Unit> Activated => _viewManager.Activated;

    public IObservable<Unit> Deactivated => _viewManager.Deactivated;

    public IObservable<Unit> IsAppearing => _viewManager.IsAppearing;

    public IObservable<Unit> IsDisappearing => _viewManager.IsDisappearing;

    public IObservable<LifecycleEvent> Lifecycle => _viewManager.Lifecycle;

    public CompositeDisposable ControlBindings => _viewManager.ControlBindings;

    public bool ControlsBound => _viewManager.ControlsBound;

    public bool MaintainBindings { get; set; }

    protected ContentPageBase(bool delayBindingRegistrationUntilAttached = false)
    {
        InitializeInternal(delayBindingRegistrationUntilAttached);
    }

    private void InitializeInternal(bool delayBindingRegistrationUntilAttached)
    {
        Initialize();

        SetupUserInterface();

        if (!delayBindingRegistrationUntilAttached)
        {
            _viewManager.RegisterBindings(this);
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _viewManager.OnLifecycle(LifecycleEvent.IsAppearing);
    }

    protected override void OnDisappearing()
    {
        _viewManager.OnLifecycle(LifecycleEvent.IsDisappearing);

        base.OnDisappearing();
    }

    protected override void OnPropertyChanged(string propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        _viewManager.OnVisualElementPropertyChanged<ContentPageBase<TViewModel>, TViewModel>(this, ViewModelProperty.PropertyName, propertyName);
    }

    protected virtual void Initialize()
    {
    }

    public abstract void SetupUserInterface();

    public abstract void BindControls();

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _viewManager?.Dispose();
                this.DisposeViewModel();
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