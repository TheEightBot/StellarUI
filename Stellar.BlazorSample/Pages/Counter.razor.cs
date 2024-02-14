using Microsoft.AspNetCore.Components;

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

    public Counter()
    {
        this.InitializeStellarComponent();
    }

    public override void Bind(CompositeDisposable disposables)
    {
    }

    private void IncrementCount()
    {
        ViewModel.Count++;
    }
}