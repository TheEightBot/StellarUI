using System.Runtime.CompilerServices;

namespace Stellar.UnitTests.Disposables;

public class WeakSingleAssignmentDisposableTests
{
    [Fact]
    public void Constructor_NullLifetimeScope_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new WeakSingleAssignmentDisposable(null!));
    }

    [Fact]
    public void Disposable_WhenUnset_IsNull()
    {
        var scope = new object();
        var single = new WeakSingleAssignmentDisposable(scope);

        Assert.Null(single.Disposable);

        GC.KeepAlive(scope);
    }

    [Fact]
    public void FirstAssignment_IsStored()
    {
        var scope = new object();
        var single = new WeakSingleAssignmentDisposable(scope);
        var item = new TrackingDisposable();

        single.Disposable = item;

        Assert.Same(item, single.Disposable);
        Assert.False(item.IsDisposed);

        GC.KeepAlive(scope);
    }

    [Fact]
    public void SecondAssignment_KeepsTheFirstAndDisposesTheIncomingOne()
    {
        var scope = new object();
        var single = new WeakSingleAssignmentDisposable(scope);
        var first = new TrackingDisposable();
        var second = new TrackingDisposable();

        single.Disposable = first;
        single.Disposable = second;

        // Single assignment: the original wins, the newcomer is discarded.
        Assert.Same(first, single.Disposable);
        Assert.False(first.IsDisposed);
        Assert.Equal(1, second.DisposeCount);

        GC.KeepAlive(scope);
    }

    [Fact]
    public void AssigningNull_IsIgnoredAndLeavesAnExistingValueIntact()
    {
        var scope = new object();
        var single = new WeakSingleAssignmentDisposable(scope);
        var first = new TrackingDisposable();
        single.Disposable = first;

        single.Disposable = null;

        Assert.Same(first, single.Disposable);
        Assert.False(first.IsDisposed);

        GC.KeepAlive(scope);
    }

    [Fact]
    public void Dispose_DisposesTheAssignedDisposable()
    {
        var scope = new object();
        var single = new WeakSingleAssignmentDisposable(scope);
        var item = new TrackingDisposable();
        single.Disposable = item;

        single.Dispose();

        Assert.True(single.IsDisposed);
        Assert.Equal(1, item.DisposeCount);

        GC.KeepAlive(scope);
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var scope = new object();
        var single = new WeakSingleAssignmentDisposable(scope);
        var item = new TrackingDisposable();
        single.Disposable = item;

        single.Dispose();
        single.Dispose();

        Assert.Equal(1, item.DisposeCount);

        GC.KeepAlive(scope);
    }

    [Fact]
    public void Getter_AfterDispose_ReturnsEmptyRatherThanNull()
    {
        var scope = new object();
        var single = new WeakSingleAssignmentDisposable(scope);
        single.Dispose();

        Assert.Same(System.Reactive.Disposables.Disposable.Empty, single.Disposable);

        GC.KeepAlive(scope);
    }

    [Fact]
    public void Assigning_AfterDispose_DisposesTheIncomingDisposableImmediately()
    {
        var scope = new object();
        var single = new WeakSingleAssignmentDisposable(scope);
        single.Dispose();

        var item = new TrackingDisposable();
        single.Disposable = item;

        Assert.Equal(1, item.DisposeCount);

        GC.KeepAlive(scope);
    }

    [Fact]
    public void CollectedLifetimeScope_DisposesIncomingDisposablesAndMarksDisposed()
    {
        var single = CreateWithCollectableScope();

        GcHelpers.ForceFullCollection();

        var item = new TrackingDisposable();
        single.Disposable = item;

        Assert.Equal(1, item.DisposeCount);
        Assert.True(single.IsDisposed);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakSingleAssignmentDisposable CreateWithCollectableScope() => new(new object());
}
