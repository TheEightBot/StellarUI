using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ReactiveUI.Builder;
using ReactiveUI.Maui;
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
        UseStellarComponents(mauiAppBuilder, useCustomMauiScheduler, useShortTermThreadPoolScheduler);

        mauiAppBuilder
            .Services
                .ConfigureStellarComponents(typeof(TStellarAssembly).GetTypeInfo().Assembly);

        return mauiAppBuilder;
    }

    public static MauiAppBuilder UseStellarComponents(this MauiAppBuilder mauiAppBuilder, bool useCustomMauiScheduler = true, bool useShortTermThreadPoolScheduler = true)
    {
        UseCustomMauiScheduler = useCustomMauiScheduler;
        UseShortTermThreadPoolScheduler = useShortTermThreadPoolScheduler;

        // ReactiveUI 23 replaced PlatformRegistrationManager and InitializeReactiveUI
        // with a builder. UseReactiveUI registers the MAUI platform services that
        // SetRegistrationNamespaces(RegistrationNamespace.Maui) used to select.
        // The schedulers are still assigned in MauiSchedulerInitializer below,
        // because IDispatcher cannot be resolved this early.
        Locator.CurrentMutable.InitializeSplat();
        mauiAppBuilder.UseReactiveUI(static builder => builder.WithMaui());

        mauiAppBuilder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IMauiInitializeScopedService, MauiSchedulerInitializer>());

        mauiAppBuilder
            .Services
            .AddSingleton(serviceProvider => new MauiScheduler(serviceProvider.GetRequiredService<IDispatcher>()));

        return mauiAppBuilder;
    }

    private class MauiSchedulerInitializer : IMauiInitializeScopedService
    {
        public void Initialize(IServiceProvider services)
        {
            if (UseCustomMauiScheduler)
            {
                RxSchedulers.MainThreadScheduler = services.GetRequiredService<MauiScheduler>();
            }

            if (UseShortTermThreadPoolScheduler)
            {
                RxSchedulers.TaskpoolScheduler = Schedulers.ShortTermThreadPoolScheduler;
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
