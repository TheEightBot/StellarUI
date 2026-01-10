using System;

namespace Stellar;

[AttributeUsage(AttributeTargets.Class)]
public class ServiceRegistrationAttribute : Attribute
{
    public Lifetime ServiceRegistrationType { get; set; }

    public bool RegisterInterfaces { get; set; }
    
    public Type? ServiceType { get; set; }
    
    public string? Key { get; set; }

    public ServiceRegistrationAttribute(
        Lifetime serviceRegistrationType = Lifetime.Transient,
        bool registerInterfaces = false,
        Type? serviceType = null,
        string? key = null)
    {
        ServiceRegistrationType = serviceRegistrationType;
        RegisterInterfaces = registerInterfaces;
        ServiceType = serviceType;
        Key = key;
    }
}

public enum Lifetime
{
    Transient = 0,
    Scoped = 1,
    Singleton = 2,
}
