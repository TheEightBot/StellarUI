using System.ComponentModel;

namespace Stellar.Maui.Pages;

public abstract class TabbedPageBase<TViewModel> : ReactiveTabbedPage<TViewModel>, IStellarView<TViewModel>
    where TViewModel : class
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ViewManager ViewManager { get; } = new MauiViewManager<TViewModel>();

    public IObservable<Unit> Initialized => ViewManager.Initialized;

    public IObservable<Unit> Activated => ViewManager.Activated;

    public IObservable<Unit> Deactivated => ViewManager.Deactivated;

    public IObservable<Unit> IsAppearing => ViewManager.IsAppearing;

    public IObservable<Unit> IsDisappearing => ViewManager.IsDisappearing;

    public IObservable<Unit> Disposed => ViewManager.Disposed;

    public IObservable<LifecycleEvent> Lifecycle => ViewManager.Lifecycle;

    public virtual void Initialize()
    {
    }

    public abstract void SetupUserInterface();

    public abstract void Bind(CompositeDisposable disposables);

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
        if (args.OldHandler is not null)
        {
            this.Loaded -= this.Handle_Loaded;
            this.Unloaded -= this.Handle_Unloaded;

            this.DisposeView();
        }

        if (args.NewHandler is not null)
        {
            this.Loaded -= this.Handle_Loaded;
            this.Loaded += this.Handle_Loaded;

            this.Unloaded -= this.Handle_Unloaded;
            this.Unloaded += this.Handle_Unloaded;
        }

        base.OnHandlerChanging(args);
    }

    private void Handle_Loaded(object sender, EventArgs e)
    {
        if (HotReloadService.HotReloadAware)
        {
            HotReloadService.UpdateApplicationEvent -= HandleHotReload;
            HotReloadService.UpdateApplicationEvent += HandleHotReload;
        }

        ViewManager.HandleActivated(this);
    }

    private void Handle_Unloaded(object sender, EventArgs e)
    {
        if (HotReloadService.HotReloadAware)
        {
            HotReloadService.UpdateApplicationEvent -= HandleHotReload;
        }

        ViewManager.HandleDeactivated(this);
    }

    private void HandleHotReload(Type[]? updatedTypes)
    {
        this.ReloadView();
    }

    protected override void OnPropertyChanged(string propertyName = null)
    {
        ViewManager.PropertyChanged<TabbedPageBase<TViewModel>, TViewModel>(this, propertyName);

        base.OnPropertyChanged(propertyName);
    }
}
