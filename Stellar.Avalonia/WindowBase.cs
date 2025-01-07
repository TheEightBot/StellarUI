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
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ViewManager<TViewModel> ViewManager { get; } = new AvaloniaViewManager<TViewModel>();

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
            bool resolveViewModel = true,
            bool maintain = false,
            bool delayBindingRegistrationUntilAttached = false,
            bool manuallyInitialize = false)
        {
            if (!manuallyInitialize)
            {
                this.InitializeStellarComponent(viewModel, resolveViewModel, maintain, delayBindingRegistrationUntilAttached);
            }
        }

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

            ViewManager.OnLifecycle(this, LifecycleEvent.IsAppearing);
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            ViewManager.OnLifecycle(this, LifecycleEvent.IsDisappearing);

            ViewManager.HandleDeactivated(this);

            base.OnClosing(e);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            ViewManager.PropertyChanged(this, change.Property.Name);

            base.OnPropertyChanged(change);
        }
    }
}
