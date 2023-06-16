using System.Reactive.Linq;
using ReactiveUI;
using Stellar.Blazor;
using Stellar.BlazorSample.ViewModels;

namespace Stellar.BlazorSample.Pages;

public partial class Index : InjectableComponentBase<IndexViewModel>
{
    protected override void OnInitialized()
    {
        base.OnInitialized();
        this.InitializeStellarComponent();
    }

    public override void BindControls()
    {
    }
}