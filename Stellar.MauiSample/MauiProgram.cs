using System.Text;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

namespace Stellar.MauiSample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var appBuilder =
            MauiApp
                .CreateBuilder()
                .UseMauiApp<App>()
                .ConfigureFonts(
                    fonts =>
                    {
                        fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                        fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    })
                .UseMauiCommunityToolkit()
                .UseMauiCommunityToolkitMarkup()
#if DEBUG
                .EnableHotReload()
#endif
                .UseStellarComponents<App>();

        /*
        We can add individual service registrations or all at once
        .RegisterServices<App>()
        .RegisterViewModels<App>()
        .RegisterViews<App>()
        */

#if DEBUG
        appBuilder.Logging.AddDebug();
#endif

        return
            appBuilder
                .Build()
                .ConfigureReactiveUISchedulers();
    }
}
