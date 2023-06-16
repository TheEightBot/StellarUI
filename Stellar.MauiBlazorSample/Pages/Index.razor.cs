using System.ComponentModel;
using System.Reactive.Disposables;
using Stellar.Blazor;
using Stellar.MauiBlazorSample.ViewModels;

namespace Stellar.MauiBlazorSample.Pages;

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