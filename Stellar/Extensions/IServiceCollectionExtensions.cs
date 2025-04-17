using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Stellar.Extensions;

namespace Stellar;

public static class IServiceCollectionExtensions
{
    private static readonly Type ServiceRegistrationType = typeof(ServiceRegistrationAttribute);

    public static IServiceCollection ConfigureStellarComponents<T>(this IServiceCollection services, Assembly assembly)
    {
        var registrationType = typeof(T);

        var assTypes =
            assembly
                ?.ExportedTypes
                ?.Where(ti => AttributeCache.HasAttribute<ServiceRegistrationAttribute>(ti) && ti.IsAssignableTo(registrationType) && !ti.IsAbstract)
                ?? Enumerable.Empty<Type>();

        foreach (var ti in assTypes)
        {
            var a = AttributeCache.GetAttribute<ServiceRegistrationAttribute>(ti);
            if (a != null)
            {
                RegisterType(services, ti, a);
            }
        }

        return services;
    }

    public static IServiceCollection ConfigureStellarComponents(this IServiceCollection services, Assembly assembly)
    {
        var assTypes =
            assembly
                ?.ExportedTypes
                ?.Where(ti => AttributeCache.HasAttribute<ServiceRegistrationAttribute>(ti))
                ?? Enumerable.Empty<Type>();

        foreach (var ti in assTypes)
        {
            var a = AttributeCache.GetAttribute<ServiceRegistrationAttribute>(ti);
            if (a != null)
            {
                RegisterType(services, ti, a);
            }
        }

        return services;
    }

    private static void RegisterType(IServiceCollection services, Type type, ServiceRegistrationAttribute attribute)
    {
        if (attribute.RegisterInterfaces)
        {
            var interfaces = type.GetInterfaces() ?? Enumerable.Empty<Type>();

            if (interfaces.Any())
            {
                foreach (var currInterface in interfaces)
                {
                    RegisterServiceByLifetime(services, attribute.ServiceRegistrationType, currInterface, type);
                }
            }
        }

        RegisterServiceByLifetime(services, attribute.ServiceRegistrationType, type, type);
    }

    private static void RegisterServiceByLifetime(IServiceCollection services, Lifetime lifetime, Type serviceType, Type implementationType)
    {
        switch (lifetime)
        {
            case Lifetime.Transient:
                services.AddTransient(serviceType, implementationType);
                break;
            case Lifetime.Scoped:
                services.AddScoped(serviceType, implementationType);
                break;
            case Lifetime.Singleton:
                services.AddSingleton(serviceType, implementationType);
                break;
        }
    }
}