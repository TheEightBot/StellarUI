using System.ComponentModel;

namespace EightBot.Stellar.Maui.Pages;

public abstract class FlyoutPageBase<TViewModel> : ReactiveMasterDetailPage<TViewModel>, IStellarView<TViewModel>
    where TViewModel : class
{
    private bool _isDisposed;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public ViewManager ViewManager { get; set; } = new();

    public IObservable<Unit> Activated => ViewManager.Activated;

    public IObservable<Unit> Deactivated => ViewManager.Deactivated;

    public IObservable<Unit> IsAppearing => ViewManager.IsAppearing;

    public IObservable<Unit> IsDisappearing => ViewManager.IsDisappearing;

    public IObservable<LifecycleEvent> Lifecycle => ViewManager.Lifecycle;

    public CompositeDisposable ControlBindings => ViewManager.ControlBindings;

    public bool MaintainBindings
    {
        get => ViewManager.MaintainBindings;
        set => ViewManager.MaintainBindings = value;
    }

    public virtual void Initialize()
    {
    }

    public abstract void SetupUserInterface();

    public abstract void BindControls();

    protected override void OnAppearing()
    {
        base.OnAppearing();

        ViewManager.OnLifecycle(LifecycleEvent.IsAppearing);
    }

    protected override void OnDisappearing()
    {
        ViewManager.OnLifecycle(LifecycleEvent.IsDisappearing);

        base.OnDisappearing();
    }

    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        ViewManager.HandlerChanging<FlyoutPageBase<TViewModel>, TViewModel>(this, args);

        base.OnHandlerChanging(args);
    }

    protected override void OnPropertyChanged(string propertyName = null)
    {
        ViewManager.PropertyChanged<FlyoutPageBase<TViewModel>, TViewModel>(this, propertyName);

        base.OnPropertyChanged(propertyName);
    }

    protected virtual void Dispose(bool disposing) =>
        this.ManageDispose(disposing, ref _isDisposed);

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}