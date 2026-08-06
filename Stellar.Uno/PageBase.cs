using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Stellar.Uno;

// Implements IViewFor directly: there is no ReactiveUI 24-compatible Uno platform
// package to supply a ReactivePage base class, so Stellar provides the ViewModel
// dependency property and DataContext syncing itself.
public abstract class PageBase<TViewModel> : Page, IStellarView<TViewModel>
    where TViewModel : class
{
    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(TViewModel),
        typeof(PageBase<TViewModel>),
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

    private void HandleDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        ViewModel = args.NewValue as TViewModel;
    }
}
