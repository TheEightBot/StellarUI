using System.ComponentModel;
using System.Windows;
using ReactiveUI.Reactive;

namespace Stellar.Wpf;

// ReactiveUserControl supplies the ViewModel dependency property and IViewFor plumbing;
// Stellar layers its ViewManager lifecycle on top. WPF has no virtual for Loaded/Unloaded,
// so the constructor subscribes to the routed events instead.
public abstract class UserControlBase<TViewModel> : ReactiveUserControl<TViewModel>, IStellarView<TViewModel>
    where TViewModel : class
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ViewManager<TViewModel> ViewManager { get; } = new WpfViewManager<TViewModel>();

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

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        ViewManager.PropertyChanged(this, e.Property.Name);

        base.OnPropertyChanged(e);
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

public abstract class UserControlBase<TViewModel, TDataModel> : ReactiveUserControl<TViewModel>, IStellarView<TViewModel>
    where TViewModel : class
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ViewManager<TViewModel> ViewManager { get; } = new WpfViewManager<TViewModel>();

    public IObservable<Unit> UserControlInitialized => ViewManager.Initialized;

    public IObservable<Unit> Activated => ViewManager.Activated;

    public IObservable<Unit> Deactivated => ViewManager.Deactivated;

    public IObservable<Unit> Disposed => ViewManager.Disposed;

    public IObservable<LifecycleEvent> LifecycleEvents => ViewManager.LifecycleEvents;

    protected UserControlBase()
    {
        Loaded += HandleLoaded;
        Unloaded += HandleUnloaded;
        DataContextChanged += HandleDataContextChanged;
    }

    public virtual void Initialize()
    {
    }

    public abstract void SetupUserInterface();

    public abstract void Bind(WeakCompositeDisposable disposables);

    protected abstract void MapDataModelToViewModel(TViewModel viewModel, TDataModel dataModel);

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        ViewManager.PropertyChanged(this, e.Property.Name);

        base.OnPropertyChanged(e);
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

    private void HandleDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (ViewModel is not null && e.NewValue is TDataModel dataModel)
        {
            MapDataModelToViewModel(ViewModel, dataModel);
        }
    }
}
