using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables;
using Avalonia;
using Avalonia.ReactiveUI;

namespace Stellar.Avalonia;

public abstract class UserControlBase<TViewModel> : ReactiveUserControl<TViewModel>, IStellarView<TViewModel>
    where TViewModel : class
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ViewManager ViewManager { get; } = new AvaloniaViewManager<TViewModel>();

    public IObservable<Unit> UserControlInitialized => ViewManager.Initialized;

    public IObservable<Unit> Activated => ViewManager.Activated;

    public IObservable<Unit> Deactivated => ViewManager.Deactivated;

    public IObservable<Unit> Disposed => ViewManager.Disposed;

    public IObservable<LifecycleEvent> Lifecycle => ViewManager.Lifecycle;

    public virtual void Initialize()
    {
    }

    public abstract void SetupUserInterface();

    public abstract void Bind(CompositeDisposable disposables);

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        ViewManager.HandleActivated(this);

        ViewManager.OnLifecycle(LifecycleEvent.IsAppearing);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ViewManager.OnLifecycle(LifecycleEvent.IsDisappearing);

        ViewManager.HandleDeactivated(this);

        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        ViewManager.PropertyChanged<UserControlBase<TViewModel>, TViewModel>(this, change.Property.Name);

        base.OnPropertyChanged(change);
    }
}

public abstract class UserControlBase<TViewModel, TDataModel> : ReactiveUserControl<TViewModel>, IStellarView<TViewModel>
    where TViewModel : class
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ViewManager ViewManager { get; } = new AvaloniaViewManager<TViewModel>();

    public IObservable<Unit> UserControlInitialized => ViewManager.Initialized;

    public IObservable<Unit> Activated => ViewManager.Activated;

    public IObservable<Unit> Deactivated => ViewManager.Deactivated;

    public IObservable<Unit> Disposed => ViewManager.Disposed;

    public IObservable<LifecycleEvent> Lifecycle => ViewManager.Lifecycle;

    public virtual void Initialize()
    {
    }

    public abstract void SetupUserInterface();

    public abstract void Bind(CompositeDisposable disposables);

    protected abstract void MapDataModelToViewModel(TViewModel viewModel, TDataModel dataModel);

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        ViewManager.HandleActivated(this);

        ViewManager.OnLifecycle(LifecycleEvent.IsAppearing);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ViewManager.OnLifecycle(LifecycleEvent.IsDisappearing);

        ViewManager.HandleDeactivated(this);

        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        ViewManager.PropertyChanged<UserControlBase<TViewModel, TDataModel>, TViewModel>(this, change.Property.Name);

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
