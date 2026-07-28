// StellarUI continues to support ListView for as long as .NET MAUI ships it. MAUI marks
// ListView and its cell types obsolete in favour of CollectionView, but removing this
// surface would break every consumer still using it, so the deprecation is suppressed here
// rather than propagated. Revisit when MAUI actually removes the types.
#pragma warning disable CS0618 // Type or member is obsolete

using System.ComponentModel;

namespace Stellar.Maui.Views;

public abstract class ViewCellBase<TViewModel> : ReactiveViewCell<TViewModel>, IStellarView<TViewModel>
    where TViewModel : class
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ViewManager<TViewModel> ViewManager { get; } = new MauiViewManager<TViewModel>();

    public IObservable<Unit> Initialized => ViewManager.Initialized;

    public IObservable<Unit> Activated => ViewManager.Activated;

    public IObservable<Unit> Deactivated => ViewManager.Deactivated;

    public IObservable<Unit> IsAppearing => ViewManager.IsAppearing;

    public IObservable<Unit> IsDisappearing => ViewManager.IsDisappearing;

    public IObservable<Unit> Disposed => ViewManager.Disposed;

    public IObservable<LifecycleEvent> LifecycleEvents => ViewManager.LifecycleEvents;

    protected ViewCellBase()
        : this(manuallyInitialize: true)
    {
    }

    protected ViewCellBase(
        TViewModel? viewModel = null,
        bool maintain = false,
        bool delayBindingRegistrationUntilAttached = false,
        bool manuallyInitialize = true)
    {
        if (!manuallyInitialize)
        {
            this.InitializeStellarComponent(viewModel, maintain, delayBindingRegistrationUntilAttached);
        }
    }

    public virtual void Initialize()
    {
    }

    public abstract void SetupUserInterface();

    public abstract void Bind(WeakCompositeDisposable disposables);

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        ViewManager.PropertyChanged(this, propertyName);

        base.OnPropertyChanged(propertyName);
    }
}

public abstract class ViewCellBase<TViewModel, TDataModel> : ReactiveViewCell<TViewModel>, IStellarView<TViewModel>
    where TViewModel : class
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ViewManager<TViewModel> ViewManager { get; } = new MauiViewManager<TViewModel>();

    public IObservable<Unit> Initialized => ViewManager.Initialized;

    public IObservable<Unit> Activated => ViewManager.Activated;

    public IObservable<Unit> Deactivated => ViewManager.Deactivated;

    public IObservable<Unit> IsAppearing => ViewManager.IsAppearing;

    public IObservable<Unit> IsDisappearing => ViewManager.IsDisappearing;

    public IObservable<Unit> Disposed => ViewManager.Disposed;

    public IObservable<LifecycleEvent> LifecycleEvents => ViewManager.LifecycleEvents;

    protected ViewCellBase()
        : this(manuallyInitialize: true)
    {
    }

    protected ViewCellBase(
        TViewModel? viewModel = null,
        bool maintain = false,
        bool delayBindingRegistrationUntilAttached = false,
        bool manuallyInitialize = true)
    {
        if (!manuallyInitialize)
        {
            this.InitializeStellarComponent(viewModel, maintain, delayBindingRegistrationUntilAttached);
        }
    }

    public virtual void Initialize()
    {
    }

    public abstract void SetupUserInterface();

    public abstract void Bind(WeakCompositeDisposable disposables);

    protected abstract void MapDataModelToViewModel(TViewModel viewModel, TDataModel dataModel);

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        ViewManager.PropertyChanged(this, propertyName);

        base.OnPropertyChanged(propertyName);
    }

    protected override void OnBindingContextChanged()
    {
        if (BindingContext is TViewModel)
        {
            base.OnBindingContextChanged();
            return;
        }

        if (ViewModel is not null && BindingContext is TDataModel dataModel)
        {
            MapDataModelToViewModel(ViewModel, dataModel);
        }
    }
}

#pragma warning restore CS0618
