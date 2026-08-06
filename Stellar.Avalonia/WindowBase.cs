using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables;
using Avalonia;
using Avalonia.Controls;
using ReactiveUI;
using Stellar.ViewModel;

namespace Stellar.Avalonia
{
    // Derives from Window directly and implements IViewFor itself: this base class
    // previously came from Avalonia.ReactiveUI's ReactiveWindow, which has no release
    // compatible with ReactiveUI 24.
    public abstract class WindowBase<TViewModel> : Window, IStellarView<TViewModel>
        where TViewModel : class
    {
        public static readonly StyledProperty<TViewModel?> ViewModelProperty =
            AvaloniaProperty.Register<WindowBase<TViewModel>, TViewModel?>(nameof(ViewModel));

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
}
