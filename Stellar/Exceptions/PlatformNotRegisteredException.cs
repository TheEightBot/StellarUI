namespace Stellar.Exceptions;

public class PlatformNotRegisteredException : Exception
{
    public PlatformNotRegisteredException()
    {
    }

    public PlatformNotRegisteredException(string message)
        : base(message)
    {
    }

    public PlatformNotRegisteredException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}