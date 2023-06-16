using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Stellar.ViewModel;

namespace Stellar.MauiBlazorSample.ViewModels;

[ServiceRegistration(Lifetime.Singleton)]
public class IndexViewModel : ViewModelBase
{
    [Reactive]
    public string? Interval { get; set; }

    protected override void RegisterObservables()
    {
        Observable
            .Interval(TimeSpan.FromSeconds(1))
            .Select(interval => $"Interval - {interval}")
            .BindTo(this, x => x.Interval)
            .DisposeWith(ViewModelBindings);
    }
}