using System.Text;
using CommunityToolkit.Maui;

namespace Stellar.MauiSample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var appBuilder =
            MauiApp
            .CreateBuilder();

        return appBuilder
            .UseMauiApp<App>()
            .ConfigureFonts(
                fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
            .UseMauiCommunityToolkit()
            .UseMauiCommunityToolkitMarkup()
            .PreCacheComponents<App>()
            /*
            We can add individual service registrations or all at once
            .RegisterServices<App>()
            .RegisterViewModels<App>()
            .RegisterViews<App>()
            */

            .AddRegisteredServices<App>()
            .Build()
            .ConfigureReactiveUISchedulers();
    }
}
