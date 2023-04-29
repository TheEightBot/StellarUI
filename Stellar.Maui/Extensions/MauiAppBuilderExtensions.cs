using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Hosting;
using Stellar.ViewModel;

namespace Stellar.Maui;

public static class MauiAppBuilderExtensions
{
    private static Type ServiceRegistrationType = typeof(ServiceRegistrationAttribute);

    public static MauiAppBuilder PreCacheComponents<TStellarAssembly>(this MauiAppBuilder mauiAppBuilder)
    {
        PreCache(mauiAppBuilder, typeof(TStellarAssembly).GetTypeInfo().Assembly);

        return mauiAppBuilder;
    }

    public static MauiAppBuilder ConfigureStellarComponents<TStellarAssembly>(this MauiAppBuilder mauiAppBuilder)
    {
        ConfigureStellarComponents(mauiAppBuilder, typeof(TStellarAssembly).GetTypeInfo().Assembly);

        return mauiAppBuilder;
    }

    public static MauiAppBuilder ConfigureStellarViewModels<TStellarAssembly>(this MauiAppBuilder mauiAppBuilder)
    {
        ConfigureStellarComponents<IViewModel>(mauiAppBuilder, typeof(TStellarAssembly).GetTypeInfo().Assembly);

        return mauiAppBuilder;
    }

    public static MauiAppBuilder ConfigureStellarServices<TStellarAssembly>(this MauiAppBuilder mauiAppBuilder)
    {
        ConfigureStellarComponents<IService>(mauiAppBuilder, typeof(TStellarAssembly).GetTypeInfo().Assembly);

        return mauiAppBuilder;
    }

    public static MauiAppBuilder ConfigureStellarViews<TStellarAssembly>(this MauiAppBuilder mauiAppBuilder)
    {
        ConfigureStellarComponents<IStellarView>(mauiAppBuilder, typeof(TStellarAssembly).GetTypeInfo().Assembly);

        return mauiAppBuilder;
    }

    private static MauiAppBuilder ConfigureStellarComponents<T>(this MauiAppBuilder mauiAppBuilder, Assembly assembly)
    {
        var registrationType = typeof(T);

        var assTypes =
            assembly
                ?.ExportedTypes
                ?.Where(ti => Attribute.IsDefined(ti, ServiceRegistrationType) && ti.IsAssignableTo(registrationType) && !ti.IsAbstract)
                ?? Enumerable.Empty<Type>();

        foreach (var ti in assTypes)
        {
            if (Attribute.GetCustomAttribute(ti, ServiceRegistrationType) is ServiceRegistrationAttribute a)
            {
                switch (a.ServiceRegistrationType)
                {
                    case Maui.Lifetime.Transient:
                        mauiAppBuilder.Services.AddTransient(ti);
                        break;
                    case Maui.Lifetime.Scoped:
                        mauiAppBuilder.Services.AddScoped(ti);
                        break;
                    case Maui.Lifetime.Singleton:
                        mauiAppBuilder.Services.AddSingleton(ti);
                        break;
                }
            }
        }

        return mauiAppBuilder;
    }

    private static MauiAppBuilder ConfigureStellarComponents(this MauiAppBuilder mauiAppBuilder, Assembly assembly)
    {
        var assTypes =
            assembly
                ?.ExportedTypes
                ?.Where(ti => Attribute.IsDefined(ti, ServiceRegistrationType))
                ?? Enumerable.Empty<Type>();

        foreach (var ti in assTypes)
        {
            if (Attribute.GetCustomAttribute(ti, ServiceRegistrationType) is ServiceRegistrationAttribute a)
            {
                switch (a.ServiceRegistrationType)
                {
                    case Maui.Lifetime.Transient:
                        mauiAppBuilder.Services.AddTransient(ti);
                        break;
                    case Maui.Lifetime.Scoped:
                        mauiAppBuilder.Services.AddScoped(ti);
                        break;
                    case Maui.Lifetime.Singleton:
                        mauiAppBuilder.Services.AddSingleton(ti);
                        break;
                }
            }
        }

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