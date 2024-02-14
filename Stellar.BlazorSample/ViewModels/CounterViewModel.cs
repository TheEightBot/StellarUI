using System;
using ReactiveUI.Fody.Helpers;
using Stellar.ViewModel;

namespace Stellar.BlazorSample.ViewModels;

[ServiceRegistration]
public class CounterViewModel : ViewModelBase
{
    [Reactive]
    public int Count { get; set; }

    protected override void Bind(CompositeDisposable disposables)
    {
    }
}