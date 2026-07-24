using Microsoft.AspNetCore.Components;
using Stellar.MauiBlazorHybridSample.ViewModels;

namespace Stellar.MauiBlazorHybridSample.Components.Pages;

public partial class Counter
{
    // Blazor assigns [Parameter] members directly, so they must be auto-properties.
    // The route value is pushed into the view model from OnParametersSet rather than
    // proxied through the property body, which is what BL0007 warns about.
    [Parameter]
    public long Count { get; set; }

    public Counter(CounterViewModel viewModel)
    {
        this.InitializeStellarComponent(viewModel);
    }

    public override void Bind(WeakCompositeDisposable disposables)
    {
        // The interval runs until the component goes away, so the subscription has to be
        // handed to the disposables the framework passes in. Without that it outlives the
        // component and keeps it alive.
        Observable
            .Interval(TimeSpan.FromSeconds(1), RxSchedulers.TaskpoolScheduler)
            .Do(i => ViewModel!.Count += i)
            .Subscribe()
            .DisposeWith(disposables);
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
