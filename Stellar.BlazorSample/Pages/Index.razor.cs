using Stellar.BlazorSample.ViewModels;

namespace Stellar.BlazorSample.Pages;

public partial class Index
{
    public Index(IndexViewModel viewModel)
    {
        this.InitializeStellarComponent(viewModel);
    }

    public override void Bind(WeakCompositeDisposable disposables)
    {
    }
}
