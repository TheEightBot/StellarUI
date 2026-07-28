using System.Runtime.CompilerServices;

namespace Stellar.UnitTests.Disposables;

public class WeakCompositeDisposableTests
{
    [Fact]
    public void Constructor_NullLifetimeScope_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new WeakCompositeDisposable(null!));
    }

    [Fact]
    public void Add_IncreasesCountAndIsContained()
    {
        var scope = new object();
        var composite = new WeakCompositeDisposable(scope);
        var item = new TrackingDisposable();

        composite.Add(item);

        Assert.Equal(1, composite.Count);
        Assert.True(composite.Contains(item));
        Assert.False(item.IsDisposed);

        GC.KeepAlive(scope);
    }

    [Fact]
    public void Add_Null_Throws()
    {
        var scope = new object();
        var composite = new WeakCompositeDisposable(scope);

        Assert.Throws<ArgumentNullException>(() => composite.Add(null!));

        GC.KeepAlive(scope);
    }

    [Fact]
    public void Add_AfterDispose_DisposesItemImmediatelyAndDoesNotStoreIt()
    {
        var scope = new object();
        var composite = new WeakCompositeDisposable(scope);
        composite.Dispose();

        var item = new TrackingDisposable();
        composite.Add(item);

        Assert.Equal(1, item.DisposeCount);
        Assert.Equal(0, composite.Count);
        Assert.False(composite.Contains(item));

        GC.KeepAlive(scope);
    }

    [Fact]
    public void Remove_DisposesTheRemovedItem()
    {
        var scope = new object();
        var composite = new WeakCompositeDisposable(scope);
        var item = new TrackingDisposable();
        composite.Add(item);

        var removed = composite.Remove(item);

        Assert.True(removed);
        Assert.Equal(1, item.DisposeCount);
        Assert.Equal(0, composite.Count);

        GC.KeepAlive(scope);
    }

    [Fact]
    public void Remove_ItemNotPresent_ReturnsFalseAndDoesNotDispose()
    {
        var scope = new object();
        var composite = new WeakCompositeDisposable(scope);
        var absent = new TrackingDisposable();

        Assert.False(composite.Remove(absent));
        Assert.False(absent.IsDisposed);

        GC.KeepAlive(scope);
    }

    [Fact]
    public void Remove_AfterDispose_ReturnsFalse()
    {
        var scope = new object();
        var composite = new WeakCompositeDisposable(scope);
        var item = new TrackingDisposable();
        composite.Add(item);
        composite.Dispose();

        Assert.False(composite.Remove(item));

        GC.KeepAlive(scope);
    }

    [Fact]
    public void Clear_DisposesEverythingButLeavesCompositeUsable()
    {
        var scope = new object();
        var composite = new WeakCompositeDisposable(scope);
        var first = new TrackingDisposable();
        var second = new TrackingDisposable();
        composite.Add(first);
        composite.Add(second);

        composite.Clear();

        Assert.Equal(1, first.DisposeCount);
        Assert.Equal(1, second.DisposeCount);
        Assert.Equal(0, composite.Count);
        Assert.False(composite.IsDisposed);

        // Clear is not Dispose: the composite still accepts new items.
        var third = new TrackingDisposable();
        composite.Add(third);
        Assert.Equal(1, composite.Count);
        Assert.False(third.IsDisposed);

        GC.KeepAlive(scope);
    }

    [Fact]
    public void Dispose_DisposesAllContainedItems()
    {
        var scope = new object();
        var composite = new WeakCompositeDisposable(scope);
        var first = new TrackingDisposable();
        var second = new TrackingDisposable();
        composite.Add(first);
        composite.Add(second);

        composite.Dispose();

        Assert.True(composite.IsDisposed);
        Assert.Equal(1, first.DisposeCount);
        Assert.Equal(1, second.DisposeCount);
        Assert.Equal(0, composite.Count);

        GC.KeepAlive(scope);
    }

    [Fact]
    public void Dispose_IsIdempotent_AndDoesNotDisposeItemsTwice()
    {
        var scope = new object();
        var composite = new WeakCompositeDisposable(scope);
        var item = new TrackingDisposable();
        composite.Add(item);

        composite.Dispose();
        composite.Dispose();
        composite.Dispose();

        Assert.Equal(1, item.DisposeCount);

        GC.KeepAlive(scope);
    }

    [Fact]
    public void Contains_AfterDispose_ReturnsFalse()
    {
        var scope = new object();
        var composite = new WeakCompositeDisposable(scope);
        var item = new TrackingDisposable();
        composite.Add(item);
        composite.Dispose();

        Assert.False(composite.Contains(item));

        GC.KeepAlive(scope);
    }

    [Fact]
    public void CopyTo_CopiesContainedItems()
    {
        var scope = new object();
        var composite = new WeakCompositeDisposable(scope);
        var first = new TrackingDisposable();
        var second = new TrackingDisposable();
        composite.Add(first);
        composite.Add(second);

        var target = new IDisposable[2];
        composite.CopyTo(target, 0);

        Assert.Contains(first, target);
        Assert.Contains(second, target);

        GC.KeepAlive(scope);
    }

    [Fact]
    public void CopyTo_NullArray_Throws()
    {
        var scope = new object();
        var composite = new WeakCompositeDisposable(scope);

        Assert.Throws<ArgumentNullException>(() => composite.CopyTo(null!, 0));

        GC.KeepAlive(scope);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    public void CopyTo_IndexOutsideArray_Throws(int arrayIndex)
    {
        var scope = new object();
        var composite = new WeakCompositeDisposable(scope);
        var target = new IDisposable[2];

        Assert.Throws<ArgumentOutOfRangeException>(() => composite.CopyTo(target, arrayIndex));

        GC.KeepAlive(scope);
    }

    [Fact]
    public void CopyTo_InsufficientSpace_Throws()
    {
        var scope = new object();
        var composite = new WeakCompositeDisposable(scope);
        composite.Add(new TrackingDisposable());
        composite.Add(new TrackingDisposable());

        var target = new IDisposable[2];

        Assert.Throws<ArgumentOutOfRangeException>(() => composite.CopyTo(target, 1));

        GC.KeepAlive(scope);
    }

    [Fact]
    public void GetEnumerator_IsASnapshot_SoMutationDuringIterationDoesNotThrow()
    {
        var scope = new object();
        var composite = new WeakCompositeDisposable(scope);
        var first = new TrackingDisposable();
        composite.Add(first);

        var seen = 0;
        foreach (var item in composite)
        {
            Assert.NotNull(item);

            // Mutating the underlying collection mid-iteration must not invalidate
            // the enumerator, because enumeration works over a copy.
            composite.Add(new TrackingDisposable());
            seen++;
        }

        Assert.Equal(1, seen);

        GC.KeepAlive(scope);
    }

    [Fact]
    public void GetEnumerator_AfterDispose_IsEmpty()
    {
        var scope = new object();
        var composite = new WeakCompositeDisposable(scope);
        composite.Add(new TrackingDisposable());
        composite.Dispose();

        Assert.Empty(composite);

        GC.KeepAlive(scope);
    }

    [Fact]
    public void IsReadOnly_IsFalse()
    {
        var scope = new object();
        var composite = new WeakCompositeDisposable(scope);

        Assert.False(composite.IsReadOnly);

        GC.KeepAlive(scope);
    }

    [Fact]
    public void CollectedLifetimeScope_MakesTheCompositeBehaveAsDisposed()
    {
        // The whole point of the type: once the object that owns the lifetime is
        // gone, the composite must stop retaining anything and dispose whatever it
        // is handed. Built in a helper so the scope has no live local root here.
        var composite = CreateWithCollectableScope();

        GcHelpers.ForceFullCollection();

        var item = new TrackingDisposable();
        composite.Add(item);

        Assert.Equal(1, item.DisposeCount);
        Assert.Equal(0, composite.Count);
        Assert.True(composite.IsDisposed);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakCompositeDisposable CreateWithCollectableScope() => new(new object());
}
