using System.Reflection;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Splat;

namespace Stellar.Avalonia;

public static class AppBuilderExtensions
{
    public static AppBuilder UseStellarComponents(this AppBuilder appBuilder)
    {
        PlatformRegistrationManager.SetRegistrationNamespaces(RegistrationNamespace.Avalonia);
        Locator.CurrentMutable.InitializeSplat();
        Locator.CurrentMutable.InitializeReactiveUI();
        RxApp.TaskpoolScheduler = Schedulers.ShortTermThreadPoolScheduler;

        return appBuilder;
    }

    public static AppBuilder EnableHotReload(this AppBuilder appBuilder)
    {
        HotReloadService.HotReloadAware = true;

        return appBuilder;
    }

    public static AppBuilder PreCacheComponents<TStellarAssembly>(this AppBuilder appBuilder)
    {
        PreCache(appBuilder, typeof(TStellarAssembly).GetTypeInfo().Assembly);

        return appBuilder;
    }

    private static Task PreCache(AppBuilder appBuilder, Assembly assembly)
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
