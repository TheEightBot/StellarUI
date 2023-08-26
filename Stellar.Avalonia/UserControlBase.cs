using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;

namespace Stellar.Avalonia;

public abstract class UserControlBase<TViewModel> : ReactiveUserControl<TViewModel>, IStellarView<TViewModel>
    where TViewModel : class
{
    private bool _isDisposed;

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

    protected virtual void Dispose(bool disposing) =>
        this.ManageDispose(disposing, ref _isDisposed);

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}