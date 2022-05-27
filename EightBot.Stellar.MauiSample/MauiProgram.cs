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
            .RegisterServices()
            .RegisterViewModels()
            .RegisterPages()
            .Build()
            .ConfigureReactiveUISchedulers(typeof(App));
    }

    public static MauiAppBuilder RegisterServices(this MauiAppBuilder builder)
    {
        builder
            .Services.AddTransient<Services.TestService>();

        return builder;
    }

    public static MauiAppBuilder RegisterViewModels(this MauiAppBuilder builder)
    {
        builder
            .Services.AddTransient<ViewModels.SampleViewModel>();

        return builder;
    }

    public static MauiAppBuilder RegisterPages(this MauiAppBuilder builder)
    {
        builder
            .Services.AddTransient<UserInterface.Pages.SamplePage>();

        return builder;
    }
}
