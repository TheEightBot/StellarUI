using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using ReactiveUI;
using Stellar.Avalonia;

namespace Stellar.AvaloniaSample.Views;

public class StellarMainWindow : WindowBase<ViewModels.MainWindowViewModel>
{
    private TextBox _text;

    public StellarMainWindow()
    {
        this.InitializeStellarComponent(new ViewModels.MainWindowViewModel());
    }

    public override void SetupUserInterface()
    {
        this.Content =
            new Grid
            {
                Children =
                {
                    (_text =
                        new TextBox
                        {
                            TextAlignment = TextAlignment.Center,
                            FontSize = 24,
                        }),
                },
            };
    }

    public override void Bind(CompositeDisposable disposables)
    {
        this.Bind(ViewModel, vm => vm.Greeting, ui => ui._text.Text)
            .DisposeWith(disposables);

        this.WhenAnyValue(x => x.ViewModel.Greeting)
            .Select(x => (x?.Length ?? 0) / 100d)
            .ObserveOn(RxApp.MainThreadScheduler)
            .BindTo(this, x => x._text.Opacity)
            .DisposeWith(disposables);
    }
}