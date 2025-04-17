using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Splat;

namespace Stellar.Maui;

public static class MauiAppBuilderExtensions
{
    public static bool UseCustomMauiScheduler { get; set; } = true;

    public static bool UseShortTermThreadPoolScheduler { get; set; } = true;

    public static MauiAppBuilder PreCacheComponents<TStellarAssembly>(this MauiAppBuilder mauiAppBuilder)
    {
        PreCache(mauiAppBuilder, typeof(TStellarAssembly).GetTypeInfo().Assembly);

        return mauiAppBuilder;
    }

    public static MauiAppBuilder UseStellarComponents<TStellarAssembly>(this MauiAppBuilder mauiAppBuilder, bool useCustomMauiScheduler = true, bool useShortTermThreadPoolScheduler = true)
    {
        UseCustomMauiScheduler = useCustomMauiScheduler;
        UseShortTermThreadPoolScheduler = useShortTermThreadPoolScheduler;

        PlatformRegistrationManager.SetRegistrationNamespaces(RegistrationNamespace.Maui);
        Locator.CurrentMutable.InitializeSplat();
        Locator.CurrentMutable.InitializeReactiveUI();

        mauiAppBuilder
            .Services
                .AddSingleton(serviceProvider => new MauiScheduler(serviceProvider.GetRequiredService<IDispatcher>()))
                .ConfigureStellarComponents(typeof(TStellarAssembly).GetTypeInfo().Assembly);

        mauiAppBuilder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IMauiInitializeScopedService, MauiSchedulerInitializer>());

        return mauiAppBuilder;
    }

    private class MauiSchedulerInitializer : IMauiInitializeScopedService
    {
        public void Initialize(IServiceProvider services)
        {
            if (UseCustomMauiScheduler)
            {
                RxApp.MainThreadScheduler = services.GetRequiredService<MauiScheduler>();
            }

            if (UseShortTermThreadPoolScheduler)
            {
                RxApp.TaskpoolScheduler = Schedulers.ShortTermThreadPoolScheduler;
            }
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
