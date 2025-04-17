using Stellar.ViewModel;

namespace Stellar.BlazorSample.ViewModels;

[ServiceRegistration(Lifetime.Singleton)]
public partial class IndexViewModel : ViewModelBase
{
    [Reactive]
    private string? _interval;

    [Reactive]
    [property: QueryParameter]
    private int _parameterInput;

    protected override void Bind(WeakCompositeDisposable disposables)
    {
        Observable
            .Interval(TimeSpan.FromSeconds(1))
            .Select(interval => $"Interval - {interval}")
            .BindTo(this, x => x.Interval)
            .DisposeWith(disposables);
    }
}
