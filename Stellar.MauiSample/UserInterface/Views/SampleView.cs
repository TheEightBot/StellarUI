using Stellar.MauiSample.ViewModels;

namespace Stellar.MauiSample.UserInterface.Views;

#pragma warning disable CA1063
public class SampleView : ContentViewBase<ViewModels.SampleViewModel>
#pragma warning restore CA1063
{
    private Label _lbl;

    public SampleView(SampleViewModel viewModel)
    {
        this.InitializeStellarComponent(viewModel);
    }

    ~SampleView()
    {
        Console.WriteLine("Finalizing Sample View");
    }

    public override void SetupUserInterface()
    {
        Content =
            new Label
            {
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center,
            }
                .Assign(out _lbl);
    }

    public override void Bind(CompositeDisposable disposables)
    {
        var colorNotifications =
            this.WhenAnyValue(static x => x.ViewModel.ColorArray)
                .IsNotNull()
                .Select(static colorArray => new Color(colorArray[0], colorArray[1], colorArray[2], colorArray[3]))
                .Replay(1)
                .RefCount();

        colorNotifications
            .Select(x => x.ToArgbHex())
            .BindTo(this, static ui => ui._lbl.Text)
            .DisposeWith(disposables);

        colorNotifications
            .BindTo(this, static ui => ui._lbl.BackgroundColor)
            .DisposeWith(disposables);
    }
}
