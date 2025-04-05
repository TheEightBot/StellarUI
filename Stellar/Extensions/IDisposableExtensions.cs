namespace Stellar;

public static class IDisposableExtensions
{
    public static TDisposable DisposeWith<TDisposable>(this TDisposable disposable, SerialDisposable serialDisposable)
        where TDisposable : IDisposable
    {
        serialDisposable.Disposable = disposable;
        return disposable;
    }

    public static TDisposable DisposeWith<TDisposable>(this TDisposable disposable, WeakCompositeDisposable disposables)
        where TDisposable : IDisposable
    {
        disposables.Add(disposable);
        return disposable;
    }

    public static TDisposable DisposeWith<TDisposable>(this TDisposable disposable, WeakSerialDisposable serialDisposable)
        where TDisposable : IDisposable
    {
        serialDisposable.Disposable = disposable;
        return disposable;
    }
}
