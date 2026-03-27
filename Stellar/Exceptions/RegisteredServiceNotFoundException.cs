namespace Stellar.Exceptions;

public sealed class RegisteredServiceNotFoundException : Exception
{
    public RegisteredServiceNotFoundException()
    {
    }

    public RegisteredServiceNotFoundException(string message)
        : base(message)
    {
    }

    public RegisteredServiceNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}