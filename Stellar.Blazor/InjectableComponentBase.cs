using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
using ReactiveUI.Blazor;

namespace Stellar.Blazor;

public abstract class InjectableComponentBase<TViewModel> : ReactiveInjectableComponentBase<TViewModel>, IStellarView<TViewModel>
    where TViewModel : class, INotifyPropertyChanged
{
    private bool _isDisposed;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public ViewManager<TViewModel> ViewManager { get; } = new BlazorViewManager<TViewModel>();

    public IObservable<Unit> Initialized => ViewManager.Initialized;

    public IObservable<Unit> IsAppearing => ViewManager.IsAppearing;

    public IObservable<Unit> IsDisappearing => ViewManager.IsDisappearing;

    public IObservable<Unit> Disposed => ViewManager.Disposed;

    public IObservable<LifecycleEvent> LifecycleEvents => ViewManager.LifecycleEvents;

    protected InjectableComponentBase()
        : this(manuallyInitialize: true)
    {
    }

    protected InjectableComponentBase(
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

    public virtual void SetupUserInterface()
    {
    }

    public abstract void Bind(CompositeDisposable disposables);

    protected override void OnInitialized()
    {
        ViewManager.HandleActivated(this);

        base.OnInitialized();
    }

    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        ViewManager.PropertyChanged(this, propertyName);

        base.OnPropertyChanged(propertyName);
    }

    protected override void Dispose(bool disposing)
    {
        this.ManageDispose(disposing, ref _isDisposed);

        base.Dispose(disposing);
    }
}
