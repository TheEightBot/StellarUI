using System.ComponentModel;
using ReactiveUI.Blazor;

namespace Stellar.Blazor;

public abstract class InjectableComponentBase<TViewModel> : ReactiveInjectableComponentBase<TViewModel>, IStellarView<TViewModel>
    where TViewModel : class, INotifyPropertyChanged
{
    private bool _isDisposed;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public ViewManager ViewManager { get; } = new BlazorViewManager();

    public bool Maintain
    {
        get => ViewManager.Maintain;
        set => ViewManager.Maintain = value;
    }

    public virtual void Initialize()
    {
    }

    public virtual void SetupUserInterface()
    {
    }

    public abstract void BindControls();

    protected override void OnInitialized()
    {
        base.OnInitialized();

        ViewManager.HandleActivated(this);
    }

    protected override void Dispose(bool disposing)
    {
        this.ManageDispose(disposing, ref _isDisposed);

        base.Dispose(disposing);
    }
}