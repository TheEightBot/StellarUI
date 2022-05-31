using System.Text;

namespace EightBot.Stellar.MauiSample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        return
            MauiApp
            .CreateBuilder()
            .UseMauiApp<App>()
            .ConfigureFonts(
                fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
            .PreCacheComponents<App>()
            .RegisterServices<App>()
            .RegisterViewModels<App>()
            .RegisterViews<App>()
            .Build()
            .ConfigureReactiveUISchedulers();
    }
}