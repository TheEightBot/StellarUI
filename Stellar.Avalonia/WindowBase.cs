using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Stellar.ViewModel;

namespace Stellar.Avalonia
{
    public abstract class WindowBase<TViewModel> : ReactiveWindow<TViewModel>, IStellarView<TViewModel>
        where TViewModel : class
    {
        private bool _isDisposed;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public ViewManager ViewManager { get; } = new AvaloniaViewManager<TViewModel>();

        public IObservable<Unit> WindowInitialized => ViewManager.Initialized;

        public IObservable<Unit> WindowActivated => ViewManager.Activated;

        public IObservable<Unit> WindowDeactivated => ViewManager.Deactivated;

        public IObservable<Unit> IsAppearing => ViewManager.IsAppearing;

        public IObservable<Unit> IsDisappearing => ViewManager.IsDisappearing;

        public IObservable<Unit> Disposed => ViewManager.Disposed;

        public IObservable<LifecycleEvent> Lifecycle => ViewManager.Lifecycle;

        public virtual void Initialize()
        {
        }

        public abstract void SetupUserInterface();

        public abstract void Bind(CompositeDisposable disposables);

        protected override void OnInitialized()
        {
            base.OnInitialized();

            ViewManager.HandleActivated(this);
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);

            ViewManager.OnLifecycle(LifecycleEvent.IsAppearing);
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            ViewManager.OnLifecycle(LifecycleEvent.IsDisappearing);

            ViewManager.HandleDeactivated(this);

            base.OnClosing(e);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            ViewManager.PropertyChanged<WindowBase<TViewModel>, TViewModel>(this, change.Property.Name);

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
}
