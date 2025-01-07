using System.ComponentModel;

namespace Stellar.Maui.Pages;

public abstract class MultiPageBase<TPage, TViewModel> : ReactiveMultiPage<TPage, TViewModel>, IStellarView<TViewModel>
    where TPage : Page
    where TViewModel : class
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ViewManager<TViewModel> ViewManager { get; } = new MauiViewManager<TViewModel>();

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

    protected MultiPageBase()
        : this(manuallyInitialize: true)
    {
    }

    protected MultiPageBase(
        TViewModel? viewModel = null,
        bool resolveViewModel = true,
        bool maintain = false,
        bool delayBindingRegistrationUntilAttached = false,
        bool manuallyInitialize = false)
    {
        if (!manuallyInitialize)
        {
            this.InitializeStellarComponent(viewModel, resolveViewModel, maintain, delayBindingRegistrationUntilAttached);
        }
    }

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

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        ViewManager.PropertyChanged(this, propertyName);

        base.OnPropertyChanged(propertyName);
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        ViewManager.OnNavigating(this, NavigationEvent.NavigatedTo);
        base.OnNavigatedTo(args);
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        ViewManager.OnNavigating(this, NavigationEvent.NavigatedFrom);
        base.OnNavigatedFrom(args);
    }
}
