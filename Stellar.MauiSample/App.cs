using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;

namespace Stellar.MauiSample;

public class App : ApplicationBase
{
    public App(UserInterface.Pages.SamplePage page)
    {
        this.On<iOS>().SetHandleControlUpdatesOnMainThread(true);
        MainPage = new Microsoft.Maui.Controls.NavigationPage(page);
    }
}
