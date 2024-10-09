using Microsoft.Extensions.Logging;

namespace Stellar.MauiBlazorHybridSample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        MauiAppBuilder builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => { fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"); });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

#if DEBUG
        builder.EnableHotReload();
#endif

        builder.UseStellarComponents<App>();
        return builder.Build();
    }
}
