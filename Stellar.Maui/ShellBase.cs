using System.ComponentModel;

namespace Stellar.Maui;

public abstract class ShellBase<TViewModel> : ReactiveShell<TViewModel>, IStellarView<TViewModel>
    where TViewModel : class
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ViewManager ViewManager { get; } = new MauiViewManager<TViewModel>();

    public IObservable<Unit> Activated => ViewManager.Activated;

    public IObservable<Unit> Deactivated => ViewManager.Deactivated;

    public IObservable<Unit> Attached => ViewManager.Attached;

    public IObservable<Unit> Detached => ViewManager.Detached;

    public IObservable<Unit> IsAppearing => ViewManager.IsAppearing;

    public IObservable<Unit> IsDisappearing => ViewManager.IsDisappearing;

    public IObservable<LifecycleEvent> Lifecycle => ViewManager.Lifecycle;

    public virtual void Initialize()
    {
    }

    public abstract void SetupUserInterface();

    public abstract void Bind(CompositeDisposable disposables);

    protected override void OnAppearing()
    {
        base.OnAppearing();

        ViewManager.OnLifecycle(this, LifecycleEvent.IsAppearing);
    }

    protected override void OnDisappearing()
    {
        ViewManager.OnLifecycle(this, LifecycleEvent.IsDisappearing);

        base.OnDisappearing();
    }

    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        ((MauiViewManager<TViewModel>)ViewManager).OnHandlerChanged(this, args);

        base.OnHandlerChanging(args);
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        ViewManager.PropertyChanged<ShellBase<TViewModel>, TViewModel>(this, propertyName);

        base.OnPropertyChanged(propertyName);
    }
}
