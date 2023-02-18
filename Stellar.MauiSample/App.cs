namespace Stellar.MauiSample;

public class App : ApplicationBase
{
    public App(UserInterface.Pages.SamplePage page)
    {
        MainPage = new NavigationPage(page);
    }
}