using System.Diagnostics.Metrics;

namespace Stellar.MauiBlazorHybridSample.ViewModels;

[ServiceRegistration]
public class CounterViewModel : ViewModelBase
{
    [Reactive]
    public long Count { get; set; }

    protected override void Bind(CompositeDisposable disposables)
    {
        Observable
            .Interval(TimeSpan.FromSeconds(2), RxApp.TaskpoolScheduler)
            .Do(i => Count *= i)
            .Subscribe();
    }
}
