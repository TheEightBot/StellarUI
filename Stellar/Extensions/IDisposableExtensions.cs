namespace Stellar;

public static class IDisposableExtensions
{
    public static IDisposable DisposeWith(this IDisposable disposable, SerialDisposable serialDisposable)
    {
        serialDisposable.Disposable = disposable;
        return disposable;
    }
}
