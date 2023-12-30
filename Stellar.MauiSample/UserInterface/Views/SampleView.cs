namespace Stellar.MauiSample.UserInterface.Views;

#pragma warning disable CA1063
public class SampleView : ContentViewBase<ViewModels.SampleViewModel>
#pragma warning restore CA1063
{
    private BoxView _box;

    public SampleView()
    {
        this.InitializeStellarComponent();
    }

    ~SampleView()
    {
        Console.WriteLine("Finalizing Sample View");
    }

    public override void SetupUserInterface()
    {
        Content =
            new BoxView
            {
            }
                .Assign(out _box);
    }

    public override void Bind(CompositeDisposable disposables)
    {
        this.WhenAnyValue(static x => x.ViewModel.ColorArray)
            .IsNotNull()
            .Select(static colorArray => new Color(colorArray[0], colorArray[1], colorArray[2], colorArray[3]))
            .BindTo(this, static ui => ui._box.BackgroundColor)
            .DisposeWith(disposables);
    }
}