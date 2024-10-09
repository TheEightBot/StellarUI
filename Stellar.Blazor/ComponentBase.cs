using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using ReactiveUI.Blazor;

namespace Stellar.Blazor;

public abstract class ComponentBase<TViewModel> : ReactiveComponentBase<TViewModel>, IStellarView<TViewModel>
    where TViewModel : class, INotifyPropertyChanged
{
    private bool _isDisposed;

    [Inject]
    private NavigationManager Navigation { get; set; }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public ViewManager ViewManager { get; } = new BlazorViewManager();

    public IObservable<Unit> Initialized => ViewManager.Initialized;

    public IObservable<Unit> IsAppearing => ViewManager.IsAppearing;

    public IObservable<Unit> IsDisappearing => ViewManager.IsDisappearing;

    public IObservable<Unit> Disposed => ViewManager.Disposed;

    public IObservable<LifecycleEvent> LifecycleEvents => ViewManager.LifecycleEvents;

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

        Navigation.LocationChanged -= LocationChanged;
        Navigation.LocationChanged += LocationChanged;

        base.OnInitialized();
    }

    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        ViewManager.PropertyChanged<ComponentBase<TViewModel>, TViewModel>(this, propertyName);

        base.OnPropertyChanged(propertyName);
    }

    private void LocationChanged(object? sender, LocationChangedEventArgs e)
    {
        string navigationMethod = e.IsNavigationIntercepted ? "HTML" : "code";
        System.Diagnostics.Debug.WriteLine($"Notified of navigation via {navigationMethod} to {e.Location}");
    }

    protected override void Dispose(bool disposing)
    {
        this.ManageDispose(disposing, ref _isDisposed);

        Navigation.LocationChanged -= LocationChanged;

        base.Dispose(disposing);
    }
}
