using System.Runtime.CompilerServices;

namespace Stellar.UnitTests.Disposables;

public class WeakSerialDisposableTests
{
    [Fact]
    public void Constructor_NullLifetimeScope_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new WeakSerialDisposable(null!));
    }

    [Fact]
    public void Disposable_WhenUnset_IsNull()
    {
        var scope = new object();
        var serial = new WeakSerialDisposable(scope);

        Assert.Null(serial.Disposable);

        GC.KeepAlive(scope);
    }

    [Fact]
    public void Assigning_StoresTheDisposable()
    {
        var scope = new object();
        var serial = new WeakSerialDisposable(scope);
        var item = new TrackingDisposable();

        serial.Disposable = item;

        Assert.Same(item, serial.Disposable);
        Assert.False(item.IsDisposed);

        GC.KeepAlive(scope);
    }

    [Fact]
    public void Reassigning_DisposesThePreviousDisposableButNotTheNewOne()
    {
        var scope = new object();
        var serial = new WeakSerialDisposable(scope);
        var first = new TrackingDisposable();
        var second = new TrackingDisposable();

        serial.Disposable = first;
        serial.Disposable = second;

        Assert.Equal(1, first.DisposeCount);
        Assert.False(second.IsDisposed);
        Assert.Same(second, serial.Disposable);

        GC.KeepAlive(scope);
    }

    [Fact]
    public void AssigningNull_DisposesThePrevious()
    {
        var scope = new object();
        var serial = new WeakSerialDisposable(scope);
        var first = new TrackingDisposable();
        serial.Disposable = first;

        serial.Disposable = null;

        Assert.Equal(1, first.DisposeCount);
        Assert.Null(serial.Disposable);

        GC.KeepAlive(scope);
    }

    [Fact]
    public void Dispose_DisposesTheCurrentDisposable()
    {
        var scope = new object();
        var serial = new WeakSerialDisposable(scope);
        var item = new TrackingDisposable();
        serial.Disposable = item;

        serial.Dispose();

        Assert.True(serial.IsDisposed);
        Assert.Equal(1, item.DisposeCount);

        GC.KeepAlive(scope);
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var scope = new object();
        var serial = new WeakSerialDisposable(scope);
        var item = new TrackingDisposable();
        serial.Disposable = item;

        serial.Dispose();
        serial.Dispose();

        Assert.Equal(1, item.DisposeCount);

        GC.KeepAlive(scope);
    }

    [Fact]
    public void Getter_AfterDispose_ReturnsEmptyRatherThanNull()
    {
        var scope = new object();
        var serial = new WeakSerialDisposable(scope);
        serial.Dispose();

        // Deliberate contract: callers can keep dereferencing without a null check.
        Assert.Same(System.Reactive.Disposables.Disposable.Empty, serial.Disposable);

        GC.KeepAlive(scope);
    }

    [Fact]
    public void Assigning_AfterDispose_DisposesTheIncomingDisposableImmediately()
    {
        var scope = new object();
        var serial = new WeakSerialDisposable(scope);
        serial.Dispose();

        var item = new TrackingDisposable();
        serial.Disposable = item;

        Assert.Equal(1, item.DisposeCount);

        GC.KeepAlive(scope);
    }

    [Fact]
    public void CollectedLifetimeScope_DisposesIncomingDisposablesAndMarksDisposed()
    {
        var serial = CreateWithCollectableScope();

        GcHelpers.ForceFullCollection();

        var item = new TrackingDisposable();
        serial.Disposable = item;

        Assert.Equal(1, item.DisposeCount);
        Assert.True(serial.IsDisposed);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakSerialDisposable CreateWithCollectableScope() => new(new object());
}
