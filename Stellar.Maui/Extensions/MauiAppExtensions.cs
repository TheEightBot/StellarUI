using System.Reflection;
using Microsoft.Maui.Dispatching;

namespace Stellar.Maui;

public static class MauiAppExtensions
{
    public static MauiApp ConfigureReactiveUISchedulers(this MauiApp mauiApp)
    {
        var dispatcher = mauiApp.Services.GetRequiredService<IDispatcher>();
        RxApp.MainThreadScheduler = new MauiScheduler(dispatcher);
        RxApp.TaskpoolScheduler = TaskPoolScheduler.Default.DisableOptimizations(typeof(ISchedulerLongRunning));

        return mauiApp;
    }
}