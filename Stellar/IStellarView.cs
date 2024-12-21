using System.ComponentModel;

namespace Stellar;

public interface IStellarView<TViewModel> : IViewFor<TViewModel>, IStellarView
    where TViewModel : class
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ViewManager<TViewModel> ViewManager { get; }
}

public interface IStellarView
{
    public void Initialize();

    public void SetupUserInterface();

    public void Bind(CompositeDisposable disposables);
}
