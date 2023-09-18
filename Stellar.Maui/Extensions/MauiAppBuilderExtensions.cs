using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;

namespace Stellar.Maui;

public static class MauiAppBuilderExtensions
{
    public static MauiAppBuilder PreCacheComponents<TStellarAssembly>(this MauiAppBuilder mauiAppBuilder)
    {
        PreCache(mauiAppBuilder, typeof(TStellarAssembly).GetTypeInfo().Assembly);

        return mauiAppBuilder;
    }

    public static MauiAppBuilder UseStellarComponents<TStellarAssembly>(this MauiAppBuilder mauiAppBuilder)
    {
        mauiAppBuilder.Services.UseMicrosoftDependencyResolver();

        PlatformRegistrationManager.SetRegistrationNamespaces(RegistrationNamespace.Maui);
        Locator.CurrentMutable.InitializeSplat();
        Locator.CurrentMutable.InitializeReactiveUI();

        RxApp.TaskpoolScheduler = Schedulers.ShortTermThreadPoolScheduler;

        mauiAppBuilder.Services.AddSingleton(
            serviceProvider =>
            {
                var dispatcher = serviceProvider.GetRequiredService<IDispatcher>();
                var mauiScheduler = new MauiScheduler(dispatcher);
                RxApp.MainThreadScheduler = mauiScheduler;
                return mauiScheduler;
            });

        mauiAppBuilder.Services.TryAddEnumerable(ServiceDescriptor.Scoped<IMauiInitializeScopedService, MauiSchedulerInitializer>());

        mauiAppBuilder.Services.ConfigureStellarComponents(typeof(TStellarAssembly).GetTypeInfo().Assembly);

        return mauiAppBuilder;
    }

    private class MauiSchedulerInitializer : IMauiInitializeScopedService
    {
        public void Initialize(IServiceProvider services)
        {
            _ = services.GetRequiredService<MauiScheduler>();
        }
    }

    public static MauiAppBuilder EnableHotReload(this MauiAppBuilder mauiAppBuilder)
    {
        HotReloadService.HotReloadAware = true;

        return mauiAppBuilder;
    }

    private static Task PreCache(MauiAppBuilder mauiAppBuilder, Assembly assembly)
    {
        if (assembly is null)
        {
            return Task.CompletedTask;
        }

        return Task.Run(
            () =>
            {
                var precacheAttribute = typeof(PreCacheAttribute);

                var assTypes =
                    assembly
                        ?.ExportedTypes
                        ?.Where(
                            ti =>
                                Attribute.IsDefined(ti, precacheAttribute) &&
                                ti.IsClass && !ti.IsAbstract &&
                                ti.GetConstructor(Type.EmptyTypes) is not null && !ti.ContainsGenericParameters)
                        ?? Enumerable.Empty<Type>();

                foreach (var ti in assTypes)
                {
                    if (ti.FullName is not null)
                    {
                        assembly!.CreateInstance(ti.FullName);
                    }
                }
            });
    }
}
