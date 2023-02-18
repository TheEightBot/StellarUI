namespace Stellar.Maui;

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
    Transient,
    Scoped,
    Singleton,
}