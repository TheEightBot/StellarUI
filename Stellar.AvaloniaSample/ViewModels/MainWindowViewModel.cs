using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Stellar.ViewModel;

namespace Stellar.AvaloniaSample.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    [Reactive]
    public string? Greeting { get; set; } = "Welcome to Avalonia!";

    protected override void Bind(CompositeDisposable disposables)
    {
    }
}