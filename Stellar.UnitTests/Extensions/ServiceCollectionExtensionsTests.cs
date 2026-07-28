using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Stellar.UnitTests.Extensions;

/// <summary>
/// ConfigureStellarComponents scans an assembly for [ServiceRegistration] and registers
/// what it finds. It runs once at startup, but getting the lifetime or the interface
/// mapping wrong produces failures a long way from here.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    private static readonly Assembly ThisAssembly = typeof(ServiceCollectionExtensionsTests).Assembly;

    private static ServiceDescriptor[] Register(Func<IServiceCollection, IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);

        // Only the fixtures declared in this file are of interest; the test assembly may
        // pick up other attributed types over time.
        return services
            .Where(d => d.ImplementationType?.DeclaringType == typeof(ServiceCollectionExtensionsTests))
            .ToArray();
    }

    [Fact]
    public void ConfigureStellarComponents_RegistersAttributedTypes()
    {
        var descriptors = Register(s => s.ConfigureStellarComponents(ThisAssembly));

        Assert.Contains(descriptors, d => d.ImplementationType == typeof(TransientThing));
        Assert.Contains(descriptors, d => d.ImplementationType == typeof(SingletonThing));
        Assert.Contains(descriptors, d => d.ImplementationType == typeof(ScopedThing));
    }

    [Fact]
    public void ConfigureStellarComponents_IgnoresTypesWithoutTheAttribute()
    {
        var descriptors = Register(s => s.ConfigureStellarComponents(ThisAssembly));

        Assert.DoesNotContain(descriptors, d => d.ImplementationType == typeof(UnattributedThing));
    }

    [Theory]
    [InlineData(typeof(TransientThing), ServiceLifetime.Transient)]
    [InlineData(typeof(ScopedThing), ServiceLifetime.Scoped)]
    [InlineData(typeof(SingletonThing), ServiceLifetime.Singleton)]
    public void ConfigureStellarComponents_UsesTheDeclaredLifetime(Type implementation, ServiceLifetime expected)
    {
        var descriptors = Register(s => s.ConfigureStellarComponents(ThisAssembly));

        var self = Assert.Single(
            descriptors,
            d => d.ImplementationType == implementation && d.ServiceType == implementation);

        Assert.Equal(expected, self.Lifetime);
    }

    [Fact]
    public void RegisterInterfaces_AlsoRegistersEachImplementedInterface()
    {
        var descriptors = Register(s => s.ConfigureStellarComponents(ThisAssembly));

        Assert.Contains(
            descriptors,
            d => d.ServiceType == typeof(IThing) && d.ImplementationType == typeof(InterfaceRegisteredThing));

        // The concrete type is always registered as well, not only the interface.
        Assert.Contains(
            descriptors,
            d => d.ServiceType == typeof(InterfaceRegisteredThing));
    }

    [Fact]
    public void WithoutRegisterInterfaces_OnlyTheConcreteTypeIsRegistered()
    {
        var descriptors = Register(s => s.ConfigureStellarComponents(ThisAssembly));

        Assert.DoesNotContain(
            descriptors,
            d => d.ServiceType == typeof(IThing) && d.ImplementationType == typeof(TransientThing));
    }

    [Fact]
    public void GenericOverload_RegistersOnlyAssignableTypes()
    {
        var descriptors = Register(s => s.ConfigureStellarComponents<IThing>(ThisAssembly));

        Assert.Contains(descriptors, d => d.ImplementationType == typeof(InterfaceRegisteredThing));

        // TransientThing does not implement IThing, so the filtered overload skips it.
        Assert.DoesNotContain(descriptors, d => d.ImplementationType == typeof(TransientThing));
    }

    [Fact]
    public void RegisteredServicesCanActuallyBeResolved()
    {
        var services = new ServiceCollection();
        services.ConfigureStellarComponents(ThisAssembly);

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<TransientThing>());
        Assert.NotNull(provider.GetRequiredService<IThing>());
    }

    [Fact]
    public void SingletonRegistrations_ResolveToTheSameInstance()
    {
        var services = new ServiceCollection();
        services.ConfigureStellarComponents(ThisAssembly);

        using var provider = services.BuildServiceProvider();

        Assert.Same(provider.GetRequiredService<SingletonThing>(), provider.GetRequiredService<SingletonThing>());
    }

    [Fact]
    public void TransientRegistrations_ResolveToDistinctInstances()
    {
        var services = new ServiceCollection();
        services.ConfigureStellarComponents(ThisAssembly);

        using var provider = services.BuildServiceProvider();

        Assert.NotSame(provider.GetRequiredService<TransientThing>(), provider.GetRequiredService<TransientThing>());
    }

    public interface IThing;

    [ServiceRegistration]
    public class TransientThing;

    [ServiceRegistration(Lifetime.Scoped)]
    public class ScopedThing;

    [ServiceRegistration(Lifetime.Singleton)]
    public class SingletonThing;

    [ServiceRegistration(Lifetime.Singleton, true)]
    public class InterfaceRegisteredThing : IThing;

    public class UnattributedThing;
}
