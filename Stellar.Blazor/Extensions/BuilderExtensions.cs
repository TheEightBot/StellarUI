using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;

namespace Stellar.Blazor;

public static class BuilderExtensions
{
    public static IServiceCollection UseStellarComponents<TStellarAssembly>(this IServiceCollection services)
    {
        services.UseMicrosoftDependencyResolver();

        PlatformRegistrationManager.SetRegistrationNamespaces(RegistrationNamespace.Blazor);
        Locator.CurrentMutable.InitializeSplat();
        Locator.CurrentMutable.InitializeReactiveUI();
        RxApp.TaskpoolScheduler = Schedulers.ShortTermThreadPoolScheduler;

        services.ConfigureStellarComponents(typeof(TStellarAssembly).GetTypeInfo().Assembly);

        return services;
    }
}