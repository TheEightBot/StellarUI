using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Stellar.ViewModel;

namespace Stellar.BlazorSample.ViewModels;

[ServiceRegistration(Lifetime.Singleton)]
public class IndexViewModel : ViewModelBase
{
    [Reactive]
    public string? Interval { get; set; }

    [QueryParameter]
    [Reactive]
    public int ParameterInput { get; set; }

    protected override void Bind(CompositeDisposable disposables)
    {
        Observable
            .Interval(TimeSpan.FromSeconds(1))
            .Select(interval => $"Interval - {interval}")
            .BindTo(this, x => x.Interval)
            .DisposeWith(disposables);
    }
}