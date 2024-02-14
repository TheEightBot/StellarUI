using System.ComponentModel;

namespace Stellar;

public interface IStellarView<TViewModel> : IViewFor<TViewModel>, IStellarView
    where TViewModel : class
{
}

public interface IStellarView
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public ViewManager ViewManager { get; }

    public void Initialize();

    public void SetupUserInterface();

    public void Bind(CompositeDisposable disposables);
}