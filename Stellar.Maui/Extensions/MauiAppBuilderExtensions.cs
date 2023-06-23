using System.Reflection;
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

        mauiAppBuilder.Services.ConfigureStellarComponents(typeof(TStellarAssembly).GetTypeInfo().Assembly);

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