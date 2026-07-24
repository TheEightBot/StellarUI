using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI.Builder;
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
        // ReactiveUI 23 replaced PlatformRegistrationManager and the
        // InitializeReactiveUI/RxApp surface with a builder. WithBlazor
        // registers the Blazor platform services and schedulers that
        // SetRegistrationNamespaces(RegistrationNamespace.Blazor) used to select.
        RxAppBuilder
            .CreateReactiveUIBuilder()
            .WithBlazor()
            .WithTaskPoolScheduler(Schedulers.ShortTermThreadPoolScheduler)
            .Build();

        return services;
    }
}
