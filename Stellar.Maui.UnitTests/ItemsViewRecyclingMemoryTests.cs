using System.Runtime.CompilerServices;
using Stellar.UnitTests;

namespace Stellar.Maui.UnitTests;

/// <summary>
/// What keeping a recycled view's bindings costs in memory. A recycler can drop a view it
/// recycled without telling anyone, so a parked view may never be rebound: it has to stay
/// collectable while parked, and whatever its bindings hold has to be let go when the list
/// leaves the window.
/// </summary>
public class ItemsViewRecyclingMemoryTests : MauiTestBase
{
    [Theory]
    [MemberData(nameof(DataModelKindsOnEachPlatform))]
    public void RecycledView_WhoseBindingsDoNotRootIt_CanBeCollectedWhileTheListIsInTheWindow(CellKind kind, FirstBind firstBind)
    {
        var list = new ListHarness();

        var cell = BindThenRecycle(list, kind, source: null, firstBind);

        GcHelpers.ForceFullCollection();

        Assert.False(cell.IsAlive);
        GC.KeepAlive(list);
    }

    [Theory]
    [MemberData(nameof(DataModelKindsOnEachPlatform))]
    public void RecycledView_RootedByALongLivedSubscription_IsReleasedWhenTheListLeavesTheWindow(CellKind kind, FirstBind firstBind)
    {
        var source = new LongLivedSource();
        var list = new ListHarness();

        var cell = BindThenRecycle(list, kind, source, firstBind);

        GcHelpers.ForceFullCollection();

        Assert.True(cell.IsAlive);
        Assert.Equal(1, source.HandlerCount);

        list.RemoveFromWindow();
        GcHelpers.ForceFullCollection();

        Assert.False(cell.IsAlive);
        Assert.Equal(0, source.HandlerCount);
        GC.KeepAlive(list);
    }

    [Theory]
    [MemberData(nameof(CellKindsOnEachPlatform))]
    public void BoundView_RootedByALongLivedSubscription_IsReleasedWhenTheListLeavesTheWindow(CellKind kind, FirstBind firstBind)
    {
        var source = new LongLivedSource();

        var (cell, window) = BindThenLeaveTheWindow(kind, source, firstBind);

        GcHelpers.ForceFullCollection();

        Assert.False(cell.IsAlive);
        Assert.Equal(0, source.HandlerCount);
        GC.KeepAlive(window);
    }

    [Fact]
    public void ViewTornDownBeyondTheCap_CanBeCollectedWhileTheListIsInTheWindow()
    {
        var source = new LongLivedSource();
        var list = new ListHarness();

        var beyond = RecycleOneMoreThanTheCap(list, source);

        GcHelpers.ForceFullCollection();

        Assert.False(beyond.IsAlive);
        Assert.Equal(Cap, source.HandlerCount);
        GC.KeepAlive(list);
    }

    [Fact]
    public void RecycledViewsThatWereCollected_GiveTheirSlotsBack()
    {
        // The views that stay are bound first: a view arriving later would itself release
        // whatever is parked, and this is about the slots of views that simply went away.
        var source = new LongLivedSource();
        var list = new ListHarness();
        var cells = new List<GridDataModelCell>();

        for (var i = 0; i < Cap; i++)
        {
            var cell = new GridDataModelCell(source);
            list.BindNew(cell, new TestItem($"Row {i}", i));
            cells.Add(cell);
        }

        RecycleUnrooted(list, Cap);
        GcHelpers.ForceFullCollection();

        foreach (var cell in cells)
        {
            list.Recycle(cell);
        }

        Assert.All(cells, static cell => Assert.True(cell.ViewManager.ControlsBound));
    }

    [Fact]
    public void ListLeavingTheWindow_ReleasesWhatIsLeftWhenSomeRecycledViewsWereCollected()
    {
        var source = new LongLivedSource();
        var list = new ListHarness();
        var kept = new GridDataModelCell(source);
        list.BindNew(kept, new TestItem("Kept", 0));

        RecycleUnrooted(list, 2);
        list.Recycle(kept);
        GcHelpers.ForceFullCollection();

        list.RemoveFromWindow();

        Assert.False(kept.ViewManager.ControlsBound);
        Assert.Equal(0, source.HandlerCount);
    }

    [Fact]
    public void List_CanBeCollectedWithViewsStillParked()
    {
        var source = new LongLivedSource();

        var list = ParkThenDropTheList(source);

        GcHelpers.ForceFullCollection();

        Assert.False(list.IsAlive);
        GC.KeepAlive(source);
    }

    // The helpers below are separate, non-inlined methods so the references they create are
    // dead by the time the caller forces a collection. They hand back a WeakReference rather
    // than a WeakReference<T> because reading IsAlive does not produce a strong reference.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference BindThenRecycle(ListHarness list, CellKind kind, LongLivedSource? source, FirstBind firstBind)
    {
        var cell = TestCells.Create(kind, source);

        list.BindNew(cell, TestCells.Row(kind, "Chair", 3, source), firstBind);
        list.Recycle(cell);

        return new WeakReference(cell);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WeakReference Cell, Window Window) BindThenLeaveTheWindow(CellKind kind, LongLivedSource source, FirstBind firstBind)
    {
        var list = new ListHarness();
        var cell = TestCells.Create(kind, source);

        list.BindNew(cell, TestCells.Row(kind, "Chair", 3, source), firstBind);
        list.RemoveFromWindow();

        return (new WeakReference(cell), list.Window);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference RecycleOneMoreThanTheCap(ListHarness list, LongLivedSource source)
    {
        var cells = new List<GridDataModelCell>();

        for (var i = 0; i <= Cap; i++)
        {
            var cell = new GridDataModelCell(source);
            list.BindNew(cell, new TestItem($"Row {i}", i));
            cells.Add(cell);
        }

        foreach (var cell in cells)
        {
            list.Recycle(cell);
        }

        return new WeakReference(cells[Cap]);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RecycleUnrooted(ListHarness list, int count)
    {
        var cells = new List<GridDataModelCell>();

        for (var i = 0; i < count; i++)
        {
            var cell = new GridDataModelCell();
            list.BindNew(cell, new TestItem($"Row {i}", i));
            cells.Add(cell);
        }

        foreach (var cell in cells)
        {
            list.Recycle(cell);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference ParkThenDropTheList(LongLivedSource source)
    {
        var list = new ListHarness();
        var cell = new GridDataModelCell(source);

        list.BindNew(cell, new TestItem("Chair", 3));
        list.Recycle(cell);

        return new WeakReference(list.List);
    }
}
