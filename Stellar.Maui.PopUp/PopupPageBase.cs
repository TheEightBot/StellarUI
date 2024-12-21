using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables;

namespace Stellar.Maui.Pages;

public abstract class PopupPageBase<TViewModel> : ReactivePopupPage<TViewModel>, IStellarView<TViewModel>
    where TViewModel : class
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ViewManager<TViewModel> ViewManager { get; set; } = new MauiViewManager<TViewModel>();

    public IObservable<Unit> Initialized => ViewManager.Initialized;

    public IObservable<Unit> Activated => ViewManager.Activated;

    public IObservable<Unit> Attached => ViewManager.Attached;

    public IObservable<Unit> IsAppearing => ViewManager.IsAppearing;

    public IObservable<Unit> IsDisappearing => ViewManager.IsDisappearing;

    public IObservable<Unit> Detached => ViewManager.Detached;

    public IObservable<Unit> Deactivated => ViewManager.Deactivated;

    public IObservable<Unit> Disposed => ViewManager.Disposed;

    public IObservable<LifecycleEvent> LifecycleEvents => ViewManager.LifecycleEvents;

    public IObservable<NavigationEvent> NavigationEvents => ViewManager.NavigationEvents;

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

    protected override void OnPropertyChanged(string propertyName = null)
    {
        ViewManager.PropertyChanged(this, propertyName);

        base.OnPropertyChanged(propertyName);
    }
}
