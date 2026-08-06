using System.ComponentModel;
using Microsoft.UI.Xaml;

namespace Stellar.WinUI;

// ReactiveUserControl supplies the ViewModel dependency property and IViewFor plumbing;
// Stellar layers its ViewManager lifecycle on top. WinUI has no all-property change
// virtual, so ViewModel changes are forwarded through RegisterPropertyChangedCallback.
public abstract class UserControlBase<TViewModel> : ReactiveUserControl<TViewModel>, IStellarView<TViewModel>
    where TViewModel : class
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ViewManager<TViewModel> ViewManager { get; } = new WinUIViewManager<TViewModel>();

    public IObservable<Unit> UserControlInitialized => ViewManager.Initialized;

    public IObservable<Unit> Activated => ViewManager.Activated;

    public IObservable<Unit> Deactivated => ViewManager.Deactivated;

    public IObservable<Unit> Disposed => ViewManager.Disposed;

    public IObservable<LifecycleEvent> LifecycleEvents => ViewManager.LifecycleEvents;

    protected UserControlBase()
        : this(manuallyInitialize: true)
    {
    }

    protected UserControlBase(
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
