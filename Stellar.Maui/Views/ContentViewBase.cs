using System.ComponentModel;

namespace Stellar.Maui.Pages;

public abstract class ContentViewBase<TViewModel> : ReactiveContentView<TViewModel>, IStellarView<TViewModel>
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

    public bool Maintain
    {
        get => ViewManager.Maintain;
        set => ViewManager.Maintain = value;
    }

    public virtual void Initialize()
    {
    }

    public abstract void SetupUserInterface();

    public abstract void BindControls();

    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        ViewManager.HandlerChanging<ContentViewBase<TViewModel>, TViewModel>(this, args);

        base.OnHandlerChanging(args);
    }

    protected override void OnPropertyChanged(string propertyName = null)
    {
        ViewManager.PropertyChanged<ContentViewBase<TViewModel>, TViewModel>(this, propertyName);
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