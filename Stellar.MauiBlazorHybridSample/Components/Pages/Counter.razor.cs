using Microsoft.AspNetCore.Components;
using Stellar.MauiBlazorHybridSample.ViewModels;

namespace Stellar.MauiBlazorHybridSample.Components.Pages;

public partial class Counter
{
    [Inject]
    public NavigationManager Navigation { get; private set; }

    [Parameter]
    public long Count
    {
        get => ViewModel.Count;
        set => ViewModel.Count = value;
    }

    public Counter(CounterViewModel viewModel)
    {
        this.InitializeStellarComponent(viewModel);
    }

    public override void Bind(WeakCompositeDisposable disposables)
    {
        Observable
            .Interval(TimeSpan.FromSeconds(1), RxApp.TaskpoolScheduler)
            .Do(i => Count += i)
            .Subscribe();
    }

    private void IncrementCount()
    {
        ViewModel.Count++;
    }

    private void Navigate()
    {
        try
        {
            Navigation.NavigateTo($"/counter/{(long)DateTime.Now.Second}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}
