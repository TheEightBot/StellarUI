using System.ComponentModel;

namespace Stellar;

public interface IStellarView<TViewModel> : IViewFor<TViewModel>, IStellarView
    where TViewModel : class
{
}

public interface IStellarView : IDisposable
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ViewManager ViewManager { get; }

    public void Initialize();

    public void SetupUserInterface();

    public void BindControls(CompositeDisposable disposables);
}