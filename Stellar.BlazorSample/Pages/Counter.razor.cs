using Microsoft.AspNetCore.Components;
using Stellar.BlazorSample.ViewModels;

namespace Stellar.BlazorSample.Pages;

public partial class Counter
{
    // Blazor assigns [Parameter] members directly, so they must be auto-properties.
    // The route value is pushed into the view model from OnParametersSet rather than
    // proxied through the property body, which is what BL0007 warns about.
    [Parameter]
    public int Count { get; set; }

    public Counter(CounterViewModel viewModel)
    {
        this.InitializeStellarComponent(viewModel);
    }

    public override void Bind(WeakCompositeDisposable disposables)
    {
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        ViewModel!.Count = Count;
    }

    private void IncrementCount()
    {
        ViewModel!.Count++;
    }
}
