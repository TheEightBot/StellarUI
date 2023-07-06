using System.ComponentModel;
using System.Runtime.CompilerServices;
using ReactiveUI.Blazor;

namespace Stellar.Blazor;

public abstract class ComponentBase<TViewModel> : ReactiveComponentBase<TViewModel>, IStellarView<TViewModel>
    where TViewModel : class, INotifyPropertyChanged
{
    private bool _isDisposed;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public ViewManager ViewManager { get; } = new BlazorViewManager();

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
        ViewManager.PropertyChanged<ComponentBase<TViewModel>, TViewModel>(this, propertyName);

        base.OnPropertyChanged(propertyName);
    }

    protected override void Dispose(bool disposing)
    {
        this.ManageDispose(disposing, ref _isDisposed);

        base.Dispose(disposing);
    }
}