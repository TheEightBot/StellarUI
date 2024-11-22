using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;

namespace Stellar.MauiSample;

public class App : ApplicationBase
{
    private UserInterface.Pages.SamplePage _page;

    public App(UserInterface.Pages.SamplePage page)
    {
        this.On<iOS>().SetHandleControlUpdatesOnMainThread(true);
        _page = page;
    }

    protected override Window CreateWindow(IActivationState activationState)
    {
        return new Window(new Microsoft.Maui.Controls.NavigationPage(_page));
    }
}
