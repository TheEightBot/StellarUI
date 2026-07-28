namespace Stellar.UnitTests;

/// <summary>
/// A disposable that records how many times it was disposed, so tests can assert
/// both that disposal happened and that it happened exactly once.
/// </summary>
internal sealed class TrackingDisposable : IDisposable
{
    private int _disposeCount;

    public int DisposeCount => Volatile.Read(ref _disposeCount);

    public bool IsDisposed => DisposeCount > 0;

    public void Dispose() => Interlocked.Increment(ref _disposeCount);
}
