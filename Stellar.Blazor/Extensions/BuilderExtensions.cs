using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Splat;

namespace Stellar.Blazor;

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
        PlatformRegistrationManager.SetRegistrationNamespaces(RegistrationNamespace.Blazor);
        Locator.CurrentMutable.InitializeSplat();
        Locator.CurrentMutable.InitializeReactiveUI();
        RxApp.TaskpoolScheduler = Schedulers.ShortTermThreadPoolScheduler;

        return services;
    }
}
