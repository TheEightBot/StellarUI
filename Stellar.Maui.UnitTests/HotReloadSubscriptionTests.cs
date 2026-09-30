using System.Reflection;

namespace Stellar.Maui.UnitTests;

/// <summary>
/// The view manager listens for hot reload only while its view is in a window. Keeping a
/// recycled view's bindings must not keep that subscription with them, because the
/// subscription is held by a static event and would root the view manager.
/// </summary>
[Collection(HotReloadCollection.Name)]
public sealed class HotReloadSubscriptionTests : MauiTestBase, IDisposable
{
    private readonly bool _original;

    public HotReloadSubscriptionTests()
    {
        _original = HotReloadService.HotReloadAware;
        HotReloadService.HotReloadAware = true;
    }

    public void Dispose() => HotReloadService.HotReloadAware = _original;

    [Theory]
    [MemberData(nameof(DataModelKindsOnEachPlatform))]
    public void Subscription_FollowsTheWindowThroughRecycleAndRebind(CellKind kind, FirstBind firstBind)
    {
        var list = new ListHarness();
        var cell = TestCells.Create(kind);

        list.BindNew(cell, TestCells.Row(kind, "Chair", 3), firstBind);

        Assert.True(IsSubscribed(cell));

        list.Recycle(cell);

        Assert.False(IsSubscribed(cell));
        Assert.True(cell.ViewManager.ControlsBound);

        list.Bind(cell, TestCells.Row(kind, "Table", 7));

        Assert.True(IsSubscribed(cell));

        list.RemoveFromWindow();

        Assert.False(IsSubscribed(cell));
    }

    [Fact]
    public void Subscription_IsDroppedWhenAViewOutsideAListLeavesTheWindow()
    {
        var page = new ContentPage();
        var window = new Window(page);
        var cell = new GridDataModelCell();

        page.Content = cell;

        Assert.True(IsSubscribed(cell));

        page.Content = null;

        Assert.False(IsSubscribed(cell));
        GC.KeepAlive(window);
    }

    private static bool IsSubscribed(ITestCell cell)
    {
        var handlers =
            (Delegate?)typeof(HotReloadService)
                .GetField(nameof(HotReloadService.UpdateApplicationEvent), BindingFlags.NonPublic | BindingFlags.Static)!
                .GetValue(null);

        return handlers?.GetInvocationList().Any(handler => ReferenceEquals(handler.Target, cell.ViewManager)) ?? false;
    }
}
