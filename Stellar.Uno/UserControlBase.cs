using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Stellar.Uno;

// Implements IViewFor directly: there is no ReactiveUI 24-compatible Uno platform
// package to supply a ReactiveUserControl base class, so Stellar provides the ViewModel
// dependency property and DataContext syncing itself.
public abstract class UserControlBase<TViewModel> : UserControl, IStellarView<TViewModel>
    where TViewModel : class
{
    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(TViewModel),
        typeof(UserControlBase<TViewModel>),
        new PropertyMetadata(null));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public ViewManager<TViewModel> ViewManager { get; } = new UnoViewManager<TViewModel>();

    public TViewModel? ViewModel
    {
        get => (TViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    object? IViewFor.ViewModel
    {
        get => ViewModel;
        set => ViewModel = value as TViewModel;
    }

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
        DataContextChanged += HandleDataContextChanged;
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

    private void HandleDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        ViewModel = args.NewValue as TViewModel;
    }
}
