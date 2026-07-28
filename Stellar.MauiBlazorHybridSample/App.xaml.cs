namespace Stellar.MauiBlazorHybridSample;

public partial class App : Application
{
    public App()
    {
        this.InitializeComponent();
    }

    // MAUI deprecated Application.MainPage in favour of overriding CreateWindow.
    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new MainPage());
}
