using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI.Builder;
using ReactiveUI.Reactive.Builder;

namespace Stellar.WinUI;

public static class BuilderExtensions
{
    public static IServiceCollection UseStellarComponents<TStellarAssembly>(this IServiceCollection services)
    {
        UseStellarComponents(services);

        services.ConfigureStellarComponents(typeof(TStellarAssembly).GetTypeInfo().Assembly);

        return services;
    }

    public static IServiceCollection UseStellarComponents(this IServiceCollection services)
    {
        // WithWinUI registers the WinUI platform services (activation fetcher,
        // main-thread scheduler, binding hooks) that ReactiveUI's old
        // InitializeReactiveUI selected.
        RxAppBuilder
            .CreateReactiveUIBuilder()
            .WithWinUI()
            .WithTaskPoolScheduler(Schedulers.ShortTermThreadPoolScheduler)
            .Build();

        return services;
    }

    public static IServiceCollection EnableHotReload(this IServiceCollection services)
    {
        HotReloadService.HotReloadAware = true;

        return services;
    }
}
