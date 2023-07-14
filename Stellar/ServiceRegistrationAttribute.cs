namespace Stellar;

[AttributeUsage(AttributeTargets.Class)]
public class ServiceRegistrationAttribute : Attribute
{
    public Lifetime ServiceRegistrationType { get; set; }

    public ServiceRegistrationAttribute(Lifetime serviceRegistrationType = Lifetime.Transient)
    {
        ServiceRegistrationType = serviceRegistrationType;
    }
}

public enum Lifetime
{
    Transient = 0,
    Scoped = 1,
    Singleton = 2,
}