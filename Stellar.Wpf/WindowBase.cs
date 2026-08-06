using System.ComponentModel;
using System.Windows;
using ReactiveUI.Reactive;

namespace Stellar.Wpf;

// ReactiveWindow supplies the ViewModel dependency property and IViewFor plumbing;
// Stellar layers its ViewManager lifecycle on top, mirroring Stellar.Avalonia's WindowBase.
public abstract class WindowBase<TViewModel> : ReactiveWindow<TViewModel>, IStellarView<TViewModel>
    where TViewModel : class
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ViewManager<TViewModel> ViewManager { get; } = new WpfViewManager<TViewModel>();

    public IObservable<Unit> WindowInitialized => ViewManager.Initialized;

    public IObservable<Unit> WindowActivated => ViewManager.Activated;

    public IObservable<Unit> WindowDeactivated => ViewManager.Deactivated;

    public IObservable<Unit> IsAppearing => ViewManager.IsAppearing;

    public IObservable<Unit> IsDisappearing => ViewManager.IsDisappearing;

    public IObservable<Unit> Disposed => ViewManager.Disposed;

    public IObservable<LifecycleEvent> LifecycleEvents => ViewManager.LifecycleEvents;

    protected WindowBase()
        : this(manuallyInitialize: true)
    {
    }

    protected WindowBase(
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

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);

        ViewManager.HandleActivated(this);
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        ViewManager.OnLifecycle(this, LifecycleEvent.IsAppearing);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        ViewManager.OnLifecycle(this, LifecycleEvent.IsDisappearing);

        ViewManager.HandleDeactivated(this);

        base.OnClosing(e);
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        ViewManager.PropertyChanged(this, e.Property.Name);

        base.OnPropertyChanged(e);
    }
}
