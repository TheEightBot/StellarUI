using Microsoft.AspNetCore.Components;
using Stellar.BlazorSample.ViewModels;

namespace Stellar.BlazorSample.Pages;

public partial class Counter
{
    [Inject]
    public NavigationManager Navigation { get; private set; }

    [Parameter]
    public int Count
    {
        get => ViewModel.Count;
        set => ViewModel.Count = value;
    }

    public Counter(CounterViewModel viewModel)
    {
        this.InitializeStellarComponent(viewModel);
    }

    public override void Bind(CompositeDisposable disposables)
    {
    }

    private void IncrementCount()
    {
        ViewModel.Count++;
    }
}
