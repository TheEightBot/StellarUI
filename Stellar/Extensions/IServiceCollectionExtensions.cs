using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

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
                ?.Where(ti => Attribute.IsDefined(ti, ServiceRegistrationType) && ti.IsAssignableTo(registrationType) && !ti.IsAbstract)
                ?? Enumerable.Empty<Type>();

        foreach (var ti in assTypes)
        {
            if (Attribute.GetCustomAttribute(ti, ServiceRegistrationType) is ServiceRegistrationAttribute a)
            {
                if (a.RegisterInterfaces)
                {
                    var interfaces = ti.GetInterfaces() ?? Enumerable.Empty<Type>();

                    if (interfaces.Any())
                    {
                        foreach (var currInterface in interfaces)
                        {
                            switch (a.ServiceRegistrationType)
                            {
                                case Lifetime.Transient:
                                    services.AddTransient(currInterface, ti);
                                    break;
                                case Lifetime.Scoped:
                                    services.AddScoped(currInterface, ti);
                                    break;
                                case Lifetime.Singleton:
                                    services.AddSingleton(currInterface, ti);
                                    break;
                            }
                        }
                    }
                }

                switch (a.ServiceRegistrationType)
                {
                    case Lifetime.Transient:
                        services.AddTransient(ti);
                        break;
                    case Lifetime.Scoped:
                        services.AddScoped(ti);
                        break;
                    case Lifetime.Singleton:
                        services.AddSingleton(ti);
                        break;
                }
            }
        }

        return services;
    }

    public static IServiceCollection ConfigureStellarComponents(this IServiceCollection services, Assembly assembly)
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
                if (a.RegisterInterfaces)
                {
                    var interfaces = ti.GetInterfaces() ?? Enumerable.Empty<Type>();

                    if (interfaces.Any())
                    {
                        foreach (var currInterface in interfaces)
                        {
                            switch (a.ServiceRegistrationType)
                            {
                                case Lifetime.Transient:
                                    services.AddTransient(currInterface, ti);
                                    break;
                                case Lifetime.Scoped:
                                    services.AddScoped(currInterface, ti);
                                    break;
                                case Lifetime.Singleton:
                                    services.AddSingleton(currInterface, ti);
                                    break;
                            }
                        }
                    }
                }

                switch (a.ServiceRegistrationType)
                {
                    case Lifetime.Transient:
                        services.AddTransient(ti);
                        break;
                    case Lifetime.Scoped:
                        services.AddScoped(ti);
                        break;
                    case Lifetime.Singleton:
                        services.AddSingleton(ti);
                        break;
                }
            }
        }

        return services;
    }
}