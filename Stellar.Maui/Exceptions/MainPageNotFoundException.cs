namespace Stellar.Maui.Exceptions;

public class MainPageNotFoundException : Exception
{
    public MainPageNotFoundException()
    {
    }

    public MainPageNotFoundException(string? message)
        : base(message)
    {
    }

    public MainPageNotFoundException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
