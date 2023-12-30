using System.ComponentModel;

namespace Stellar.Maui.Views;

public abstract class ViewCellBase<TViewModel> : ReactiveViewCell<TViewModel>, IStellarView<TViewModel>
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

    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        if (args.OldHandler is not null)
        {
            if (HotReloadService.HotReloadAware)
            {
                HotReloadService.UpdateApplicationEvent -= HandleHotReload;
            }

            ViewManager.HandleDeactivated(this);

            this.DisposeView();
        }

        if (args.NewHandler is not null)
        {
            if (HotReloadService.HotReloadAware)
            {
                HotReloadService.UpdateApplicationEvent -= HandleHotReload;
                HotReloadService.UpdateApplicationEvent += HandleHotReload;
            }

            ViewManager.HandleActivated(this);
        }

        base.OnHandlerChanging(args);
    }

    private void HandleHotReload(Type[]? updatedTypes)
    {
        this.ReloadView();
    }

    protected override void OnPropertyChanged(string propertyName = null)
    {
        ViewManager.PropertyChanged<ViewCellBase<TViewModel>, TViewModel>(this, propertyName);

        base.OnPropertyChanged(propertyName);
    }
}

public abstract class ViewCellBase<TViewModel, TDataModel> : ReactiveViewCell<TViewModel>, IStellarView<TViewModel>
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

    protected abstract void MapDataModelToViewModel(TViewModel viewModel, TDataModel dataModel);

    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        if (args.OldHandler is not null)
        {
            if (HotReloadService.HotReloadAware)
            {
                HotReloadService.UpdateApplicationEvent -= HandleHotReload;
            }

            ViewManager.HandleDeactivated(this);

            this.DisposeView();
        }

        if (args.NewHandler is not null)
        {
            if (HotReloadService.HotReloadAware)
            {
                HotReloadService.UpdateApplicationEvent -= HandleHotReload;
                HotReloadService.UpdateApplicationEvent += HandleHotReload;
            }

            ViewManager.HandleActivated(this);
        }

        base.OnHandlerChanging(args);
    }

    private void HandleHotReload(Type[]? updatedTypes)
    {
        this.ReloadView();
    }

    protected override void OnPropertyChanged(string propertyName = null)
    {
        ViewManager.PropertyChanged<ViewCellBase<TViewModel, TDataModel>, TViewModel>(this, propertyName);

        base.OnPropertyChanged(propertyName);
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (ViewModel is not null && BindingContext is TDataModel dataModel)
        {
            MapDataModelToViewModel(ViewModel, dataModel);
        }
    }
}
