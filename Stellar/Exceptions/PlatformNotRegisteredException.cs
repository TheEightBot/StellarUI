namespace Stellar.Exceptions;

public sealed class PlatformNotRegisteredException : Exception
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