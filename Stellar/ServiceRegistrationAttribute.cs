namespace Stellar;

[AttributeUsage(AttributeTargets.Class)]
public class ServiceRegistrationAttribute : Attribute
{
    public Lifetime ServiceRegistrationType { get; set; }

    public bool RegisterInterfaces { get; set; }

    public ServiceRegistrationAttribute()
    {
        ServiceRegistrationType = Lifetime.Transient;
    }

    public ServiceRegistrationAttribute(Lifetime serviceRegistrationType)
    {
        ServiceRegistrationType = serviceRegistrationType;
    }

    public ServiceRegistrationAttribute(Lifetime serviceRegistrationType = Lifetime.Transient, bool registerInterfaces = false)
    {
        ServiceRegistrationType = serviceRegistrationType;
        RegisterInterfaces = registerInterfaces;
    }
}

public enum Lifetime
{
    Transient = 0,
    Scoped = 1,
    Singleton = 2,
}