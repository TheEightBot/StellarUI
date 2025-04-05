using System.Diagnostics.Metrics;

namespace Stellar.MauiBlazorHybridSample.ViewModels;

[ServiceRegistration]
public partial class CounterViewModel : ViewModelBase
{
    [Reactive]
    private long _count;

    protected override void Bind(WeakCompositeDisposable disposables)
    {
        Observable
            .Interval(TimeSpan.FromSeconds(2), RxApp.TaskpoolScheduler)
            .Do(i => Count *= i)
            .Subscribe();
    }
}
