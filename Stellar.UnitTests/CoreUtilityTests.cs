using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Stellar.Exceptions;
using Stellar.Extensions;

namespace Stellar.UnitTests;

/// <summary>
/// The small pieces of the core surface: string helpers, the shared scheduler and
/// observable, the disposal helpers, the attribute cache and the exception types.
/// </summary>
public class CoreUtilityTests
{
    [Theory]
    [InlineData("", true)]
    [InlineData(" ", false)]
    [InlineData("a", false)]
    public void IsNullOrEmpty_MatchesTheBclSemantics(string value, bool expected)
    {
        Assert.Equal(expected, value.IsNullOrEmpty());
        Assert.Equal(!expected, value.IsNotNullOrEmpty());
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("   ", true)]
    [InlineData("\t\n", true)]
    [InlineData("a", false)]
    public void IsNullOrWhiteSpace_MatchesTheBclSemantics(string value, bool expected)
    {
        Assert.Equal(expected, value.IsNullOrWhiteSpace());
        Assert.Equal(!expected, value.IsNotNullOrWhiteSpace());
    }

    [Fact]
    public void ShortTermThreadPoolScheduler_IsCachedAndUsable()
    {
        var first = Schedulers.ShortTermThreadPoolScheduler;

        Assert.NotNull(first);

        // Backed by a static Lazy, so every caller shares one scheduler rather than
        // building a new one per access.
        Assert.Same(first, Schedulers.ShortTermThreadPoolScheduler);
    }

    [Fact]
    public void ShortTermThreadPoolScheduler_ActuallySchedulesWork()
    {
        using var ran = new ManualResetEventSlim();

        Schedulers.ShortTermThreadPoolScheduler.Schedule(() => ran.Set());

        Assert.True(ran.Wait(TimeSpan.FromSeconds(10)));
    }

    [Fact]
    public void ShortTermThreadPoolScheduler_DoesNotAdvertiseLongRunning()
    {
        // The whole point of the wrapper: long-running work must not be handed to this
        // scheduler, so the capability is deliberately switched off.
        Assert.Null(Schedulers.ShortTermThreadPoolScheduler.AsLongRunning());
    }

    [Fact]
    public void UnitDefault_EmitsASingleValueAndCompletes()
    {
        var values = Observables.UnitDefault.ToList().Wait();

        Assert.Single(values);
    }

    [Fact]
    public void DisposeWith_SerialDisposable_TracksTheDisposable()
    {
        using var serial = new SerialDisposable();
        var item = new TrackingDisposable();

        var returned = item.DisposeWith(serial);

        Assert.Same(item, returned);
        Assert.Same(item, serial.Disposable);

        serial.Dispose();
        Assert.Equal(1, item.DisposeCount);
    }

    [Fact]
    public void DisposeWith_WeakCompositeDisposable_TracksTheDisposable()
    {
        var scope = new object();
        var composite = new WeakCompositeDisposable(scope);
        var item = new TrackingDisposable();

        var returned = item.DisposeWith(composite);

        Assert.Same(item, returned);
        Assert.True(composite.Contains(item));

        composite.Dispose();
        Assert.Equal(1, item.DisposeCount);

        GC.KeepAlive(scope);
    }

    [Fact]
    public void DisposeWith_WeakSerialDisposable_TracksTheDisposable()
    {
        var scope = new object();
        var serial = new WeakSerialDisposable(scope);
        var item = new TrackingDisposable();

        var returned = item.DisposeWith(serial);

        Assert.Same(item, returned);

        serial.Dispose();
        Assert.Equal(1, item.DisposeCount);

        GC.KeepAlive(scope);
    }

    [Fact]
    public void AttributeCache_FindsAnAttributeOnAType()
    {
        var attribute = AttributeCache.GetAttribute<ServiceRegistrationAttribute>(typeof(Decorated));

        Assert.NotNull(attribute);
        Assert.Equal(Lifetime.Singleton, attribute!.ServiceRegistrationType);
    }

    [Fact]
    public void AttributeCache_ReturnsNullWhenTheAttributeIsAbsent()
    {
        Assert.Null(AttributeCache.GetAttribute<ServiceRegistrationAttribute>(typeof(Undecorated)));
    }

    [Fact]
    public void AttributeCache_ReturnsTheSameInstanceOnRepeatedLookups()
    {
        // The cache exists so view initialisation does not pay for reflection repeatedly.
        var first = AttributeCache.GetAttribute<ServiceRegistrationAttribute>(typeof(Decorated));
        var second = AttributeCache.GetAttribute<ServiceRegistrationAttribute>(typeof(Decorated));

        Assert.Same(first, second);
    }

    [Fact]
    public void AttributeCache_FindsAnAttributeOnAMember()
    {
        var member = typeof(Decorated).GetProperty(nameof(Decorated.Tagged))!;

        var attribute = AttributeCache.GetAttribute<QueryParameterAttribute>(member);

        Assert.NotNull(attribute);
        Assert.Same(attribute, AttributeCache.GetAttribute<QueryParameterAttribute>(member));
    }

    [Fact]
    public void AttributeCache_ReturnsNullForAnUndecoratedMember()
    {
        var member = typeof(Decorated).GetProperty(nameof(Decorated.Plain))!;

        Assert.Null(AttributeCache.GetAttribute<QueryParameterAttribute>(member));
    }

    [Fact]
    public void ServiceRegistrationAttribute_DefaultsToTransient()
    {
        var attribute = new ServiceRegistrationAttribute();

        Assert.Equal(Lifetime.Transient, attribute.ServiceRegistrationType);
        Assert.False(attribute.RegisterInterfaces);
    }

    [Fact]
    public void ServiceRegistrationAttribute_CarriesTheLifetimeItWasGiven()
    {
        var attribute = new ServiceRegistrationAttribute(Lifetime.Scoped);

        Assert.Equal(Lifetime.Scoped, attribute.ServiceRegistrationType);
    }

    [Fact]
    public void ServiceRegistrationAttribute_CarriesBothArguments()
    {
        var attribute = new ServiceRegistrationAttribute(Lifetime.Singleton, true);

        Assert.Equal(Lifetime.Singleton, attribute.ServiceRegistrationType);
        Assert.True(attribute.RegisterInterfaces);
    }

    [Fact]
    public void PlatformNotRegisteredException_CarriesMessageAndInnerException()
    {
        Assert.NotNull(new PlatformNotRegisteredException().Message);
        Assert.Equal("boom", new PlatformNotRegisteredException("boom").Message);

        var inner = new InvalidOperationException();
        var withInner = new PlatformNotRegisteredException("boom", inner);

        Assert.Equal("boom", withInner.Message);
        Assert.Same(inner, withInner.InnerException);
    }

    [Fact]
    public void RegisteredServiceNotFoundException_CarriesMessageAndInnerException()
    {
        Assert.NotNull(new RegisteredServiceNotFoundException().Message);
        Assert.Equal("boom", new RegisteredServiceNotFoundException("boom").Message);

        var inner = new InvalidOperationException();
        var withInner = new RegisteredServiceNotFoundException("boom", inner);

        Assert.Equal("boom", withInner.Message);
        Assert.Same(inner, withInner.InnerException);
    }

    [Fact]
    public void ValidationInformation_ThreeArgumentConstructor_MarksAnError()
    {
        var information = new ValidationInformation("Name", "is required", 42);

        Assert.Equal("Name", information.PropertyName);
        Assert.Equal("is required", information.ErrorMessage);
        Assert.Equal(42, information.AttemptedValue);
        Assert.True(information.IsError);
    }

    [Fact]
    public void ValidationInformation_PropertyOnlyConstructor_DefaultsToNotAnError()
    {
        var information = new ValidationInformation("Name");

        Assert.False(information.IsError);
        Assert.Null(information.ErrorMessage);
    }

    [Fact]
    public void DefaultValidationResult_IsValidAndEmpty()
    {
        Assert.True(ValidationResult.DefaultValidationResult.IsValid);
        Assert.Empty(ValidationResult.DefaultValidationResult.ValidationInformation);
    }

    [ServiceRegistration(Lifetime.Singleton)]
    private sealed class Decorated
    {
        [QueryParameter]
        public string? Tagged { get; set; }

        public string? Plain { get; set; }
    }

    private sealed class Undecorated;
}
