using EightBot.Stellar.ViewModel;
using Microsoft.Maui.Dispatching;

namespace EightBot.Stellar.Maui;

public static class MauiAppExtensions
{
    public static MauiApp ConfigureReactiveUISchedulers(this MauiApp mauiApp, Type appType = null)
    {
        var dispatcher = mauiApp.Services.GetRequiredService<IDispatcher>();
        RxApp.MainThreadScheduler = new MauiScheduler(dispatcher);
        RxApp.TaskpoolScheduler = TaskPoolScheduler.Default.DisableOptimizations(typeof(ISchedulerLongRunning));

        if (appType != null)
        {
            PreCache(appType);
        }

        return mauiApp;
    }

    private static Task PreCache(Type appType)
    {
        return Task.Run(
            () =>
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var sb = new System.Text.StringBuilder();
                var totalTime = 0L;

                sb
                    .AppendLine()
                    .AppendLine("------------------------------------------------------------------------------------------")
                    .AppendLine(" Precaching - Start")
                    .AppendLine("------------------------------------------------------------------------------------------");

                var viewModelType = typeof(ViewModelBase);
                var iViewForType = typeof(IViewFor);
                var precacheAttribute = typeof(PreCacheAttribute);

                var ass = appType.Assembly;

                var assTypes =
                    ass
                        .GetTypes()
                        .Where(
                            ti =>
                                Attribute.IsDefined(ti, precacheAttribute) &&
                                ti.IsClass && !ti.IsAbstract &&
                                ti.GetConstructor(Type.EmptyTypes) != null && !ti.ContainsGenericParameters)
                        .ToList();

                sb
                    .AppendLine($" Assembly Loading\t-\t{sw.ElapsedMilliseconds:N1}ms")
                    .AppendLine()
                    .AppendLine("-------------------------")
                    .AppendLine(" Precaching Views and View Models")
                    .AppendLine();

                foreach (var ti in assTypes)
                {
                    if (ti.IsSubclassOf(viewModelType) || iViewForType.IsAssignableFrom(ti))
                    {
                        try
                        {
                            sw.Restart();
                            ass.CreateInstance(ti.FullName);
                            var elapsed = sw.ElapsedMilliseconds;
                            totalTime += elapsed;
                            sb.AppendLine($" {ti.Name,-50}\t-\t{elapsed:N1}ms");
                        }
                        catch (Exception ex)
                        {
                            sb
                                .AppendLine($" {ti.Name,-50}\t-\tException: {ex}ms")
                                .AppendLine()
                                .AppendLine($"{ex}")
                                .AppendLine();
                        }
                    }
                }

                sw.Stop();

                sb
                    .AppendLine()
                    .AppendLine($" {"Total View and View Model Loading":-50} \t-\t{totalTime:N1}ms")
                    .AppendLine("-------------------------")
                    .AppendLine()
                    .AppendLine("------------------------------------------------------------------------------------------")
                    .AppendLine(" Precaching - End")
                    .AppendLine("------------------------------------------------------------------------------------------");

                System.Diagnostics.Debug.WriteLine(sw.ToString());
            })
        .ContinueWith(
            result =>
            {
                if (result.IsFaulted)
                {
                    System.Diagnostics.Debug.WriteLine(result.Exception);
                }
            });
    }
}