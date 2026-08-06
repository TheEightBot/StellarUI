using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;

namespace Stellar.WinUI;

// ReactivePage supplies the ViewModel dependency property and IViewFor plumbing; Stellar
// layers its ViewManager lifecycle on top. WinUI has no all-property change virtual, so
// ViewModel changes are forwarded through RegisterPropertyChangedCallback instead.
public abstract class PageBase<TViewModel> : ReactivePage<TViewModel>, IStellarView<TViewModel>
    where TViewModel : class
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ViewManager<TViewModel> ViewManager { get; } = new WinUIViewManager<TViewModel>();

    public IObservable<Unit> PageInitialized => ViewManager.Initialized;

    public IObservable<Unit> Activated => ViewManager.Activated;

    public IObservable<Unit> Deactivated => ViewManager.Deactivated;

    public IObservable<Unit> IsAppearing => ViewManager.IsAppearing;

    public IObservable<Unit> IsDisappearing => ViewManager.IsDisappearing;

    public IObservable<Unit> NavigatedTo => ViewManager.NavigatedTo;

    public IObservable<Unit> NavigatedFrom => ViewManager.NavigatedFrom;

    public IObservable<Unit> Disposed => ViewManager.Disposed;

    public IObservable<LifecycleEvent> LifecycleEvents => ViewManager.LifecycleEvents;

    protected PageBase()
        : this(manuallyInitialize: true)
    {
    }

    protected PageBase(
        TViewModel? viewModel = null,
        bool maintain = false,
        bool delayBindingRegistrationUntilAttached = false,
        bool manuallyInitialize = true)
    {
        Loaded += HandleLoaded;
        Unloaded += HandleUnloaded;
        RegisterPropertyChangedCallback(
            ViewModelProperty,
            (_, _) => ViewManager.PropertyChanged(this, nameof(ViewModel)));

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

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        ViewManager.OnNavigating(this, NavigationEvent.NavigatedTo);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        ViewManager.OnNavigating(this, NavigationEvent.NavigatedFrom);

        base.OnNavigatedFrom(e);
    }

    private void HandleLoaded(object sender, RoutedEventArgs e)
    {
        ViewManager.HandleActivated(this);

        ViewManager.OnLifecycle(this, LifecycleEvent.IsAppearing);
    }

    private void HandleUnloaded(object sender, RoutedEventArgs e)
    {
        ViewManager.OnLifecycle(this, LifecycleEvent.IsDisappearing);

        ViewManager.HandleDeactivated(this);
    }
}
