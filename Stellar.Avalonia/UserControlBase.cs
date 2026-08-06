using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using ReactiveUI;

namespace Stellar.Avalonia;

// Derives from UserControl directly and implements IViewFor itself: this base class
// previously came from Avalonia.ReactiveUI's ReactiveUserControl, which has no release
// compatible with ReactiveUI 24.
public abstract class UserControlBase<TViewModel> : UserControl, IStellarView<TViewModel>
    where TViewModel : class
{
    public static readonly StyledProperty<TViewModel?> ViewModelProperty =
        AvaloniaProperty.Register<UserControlBase<TViewModel>, TViewModel?>(nameof(ViewModel));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public ViewManager<TViewModel> ViewManager { get; } = new AvaloniaViewManager<TViewModel>();

    public TViewModel? ViewModel
    {
        get => GetValue(ViewModelProperty);
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

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        ViewManager.HandleActivated(this);

        ViewManager.OnLifecycle(this, LifecycleEvent.IsAppearing);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ViewManager.OnLifecycle(this, LifecycleEvent.IsDisappearing);

        ViewManager.HandleDeactivated(this);

        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        ViewModel = DataContext as TViewModel;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == ViewModelProperty)
        {
            var newViewModel = change.GetNewValue<TViewModel?>();

            if (!ReferenceEquals(newViewModel, DataContext))
            {
                DataContext = newViewModel;
            }
        }

        ViewManager.PropertyChanged(this, change.Property.Name);

        base.OnPropertyChanged(change);
    }
}

public abstract class UserControlBase<TViewModel, TDataModel> : UserControl, IStellarView<TViewModel>
    where TViewModel : class
{
    public static readonly StyledProperty<TViewModel?> ViewModelProperty =
        AvaloniaProperty.Register<UserControlBase<TViewModel, TDataModel>, TViewModel?>(nameof(ViewModel));

    [EditorBrowsable(EditorBrowsableState.Never)]
    public ViewManager<TViewModel> ViewManager { get; } = new AvaloniaViewManager<TViewModel>();

    public TViewModel? ViewModel
    {
        get => GetValue(ViewModelProperty);
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

    public virtual void Initialize()
    {
    }

    public abstract void SetupUserInterface();

    public abstract void Bind(WeakCompositeDisposable disposables);

    protected abstract void MapDataModelToViewModel(TViewModel viewModel, TDataModel dataModel);

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        ViewManager.HandleActivated(this);

        ViewManager.OnLifecycle(this, LifecycleEvent.IsAppearing);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ViewManager.OnLifecycle(this, LifecycleEvent.IsDisappearing);

        ViewManager.HandleDeactivated(this);

        base.OnDetachedFromVisualTree(e);
    }

    // No ViewModel<->DataContext syncing here: unlike the single-parameter base class,
    // DataContext holds a TDataModel that is mapped onto the view model, so the two
    // properties are intentionally independent.
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        ViewManager.PropertyChanged(this, change.Property.Name);

        base.OnPropertyChanged(change);
    }

    protected override void OnDataContextEndUpdate()
    {
        base.OnDataContextEndUpdate();

        if (ViewModel is not null && DataContext is TDataModel dataModel)
        {
            MapDataModelToViewModel(ViewModel, dataModel);
        }
    }
}
