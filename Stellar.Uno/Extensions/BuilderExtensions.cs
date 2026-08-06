using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using ReactiveUI.Reactive.Builder;

namespace Stellar.Uno;

public static class BuilderExtensions
{
    public static IServiceCollection UseStellarComponents<TStellarAssembly>(this IServiceCollection services)
    {
        UseStellarComponents(services);

        services.ConfigureStellarComponents(typeof(TStellarAssembly).GetTypeInfo().Assembly);

        return services;
    }

    /// <summary>
    /// Registers Stellar's Uno Platform services. Must be called from the UI thread
    /// (e.g. in <c>Application.OnLaunched</c>) so the UI thread's
    /// <see cref="DispatcherQueue"/> can be captured for the main-thread scheduler.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection UseStellarComponents(this IServiceCollection services)
    {
        var dispatcherQueue = DispatcherQueue.GetForCurrentThread()
            ?? throw new InvalidOperationException(
                "UseStellarComponents must be called from the UI thread so the DispatcherQueue can be captured.");

        // There is no ReactiveUI 24-compatible Uno platform package, so the activation
        // fetcher and schedulers are registered here directly, mirroring Stellar.Avalonia.
        RxAppBuilder
            .CreateReactiveUIBuilder()
            .WithRegistration(
                static resolver => resolver.RegisterConstant<IActivationForViewFetcher>(new UnoActivationForViewFetcher()))
            .WithMainThreadScheduler(new UnoScheduler(dispatcherQueue))
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
