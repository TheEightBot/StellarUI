// ViewCell is obsolete in MAUI but Stellar still ships ViewCellBase for it.
#pragma warning disable CS0618 // Type or member is obsolete

using ReactiveUI.Reactive.Maui;
using Stellar.Maui.Pages;
using Stellar.Maui.Views;

namespace Stellar.Maui.UnitTests;

/// <summary>
/// Everything that is not a direct child of an ItemsView is activated with its window and
/// torn down without it, exactly as before recycled item views started keeping their
/// bindings.
/// </summary>
public class ViewsOutsideAListTests : MauiTestBase
{
    [Theory]
    [MemberData(nameof(CellKinds))]
    public void View_IsTornDownWhenRemovedAndReboundWhenAddedAgain(CellKind kind)
    {
        var source = new LongLivedSource();
        var page = new ContentPage();
        var window = new Window(page);
        var cell = TestCells.Create(kind, source);
        ((View)cell).BindingContext = TestCells.Row(kind, "Chair", 3, source);
        page.Content = (View)cell;
        cell.Events.Clear();

        page.Content = null;

        Assert.Equal(new[] { LifecycleEvent.Detached, LifecycleEvent.Deactivated }, cell.Events);
        Assert.False(cell.ViewManager.ControlsBound);
        Assert.Equal(0, source.HandlerCount);

        cell.Events.Clear();
        page.Content = (View)cell;

        Assert.Equal(
            new[] { LifecycleEvent.Initialized, LifecycleEvent.Activated, LifecycleEvent.Attached },
            cell.Events);
        Assert.Equal(2, cell.BindCalls);
        Assert.Equal(1, source.HandlerCount);
        GC.KeepAlive(window);
    }

    [Fact]
    public void View_IsDisposedWhenRemoved()
    {
        var page = new ContentPage();
        var window = new Window(page);
        var cell = new DisposableCell();
        page.Content = cell;

        page.Content = null;

        Assert.Equal(1, cell.Disposal.DisposeCount);
        GC.KeepAlive(window);
    }

    [Fact]
    public void ViewInsideAnItemView_IsTornDownOnRecycle()
    {
        // Only the item view itself is a child of the list. A Stellar view nested inside
        // one has some other parent, and is handled as any view outside a list is.
        var source = new LongLivedSource();
        var list = new ListHarness();
        var nested = new GridDataModelCell(source);
        var item = new ContentView { Content = nested };
        nested.BindingContext = new TestItem("Chair", 3);
        list.List.AddLogicalChild(item);
        nested.Events.Clear();

        list.List.RemoveLogicalChild(item);

        Assert.Equal(new[] { LifecycleEvent.Detached, LifecycleEvent.Deactivated }, nested.Events);
        Assert.False(nested.ViewManager.ControlsBound);
        Assert.Equal(0, source.HandlerCount);
    }

    [Fact]
    public void Page_IsActivatedAndDeactivatedWithItsWindow()
    {
        var page = new TestPage();
        var events = new List<LifecycleEvent>();
        using var subscription = page.ViewManager.LifecycleEvents.Subscribe(events.Add);
        var window = new Window(new ContentPage());

        window.Page = page;

        Assert.Equal(new[] { LifecycleEvent.Activated, LifecycleEvent.Attached }, events);

        events.Clear();
        window.Page = new ContentPage();

        Assert.Equal(new[] { LifecycleEvent.Detached, LifecycleEvent.Deactivated }, events);
        Assert.False(page.ViewManager.ControlsBound);
    }

    [Fact]
    public void ViewThatIsNotAStellarView_IsLeftAlone()
    {
        using var manager = new MauiViewManager<TestCellViewModel>();
        var events = new List<LifecycleEvent>();
        using var subscription = manager.LifecycleEvents.Subscribe(events.Add);
        var view = new ReactiveContentView<TestCellViewModel>();
        var page = new ContentPage { Content = view };
        var window = new Window(page);

        manager.PropertyChanged(view, nameof(VisualElement.Window));
        page.Content = null;
        manager.PropertyChanged(view, nameof(VisualElement.Window));
        manager.PropertyChanged(view, propertyName: null);

        Assert.Empty(events);
        Assert.False(manager.ControlsBound);
        GC.KeepAlive(window);
    }

    [Fact]
    public void ViewCell_IsNotAffectedByAWindowChange()
    {
        var cell = new TestViewCell();
        var events = new List<LifecycleEvent>();
        using var subscription = cell.ViewManager.LifecycleEvents.Subscribe(events.Add);

        cell.ViewManager.PropertyChanged(cell, nameof(VisualElement.Window));
        cell.ViewManager.PropertyChanged(cell, nameof(Element.Parent));

        Assert.Empty(events);
        Assert.True(cell.ViewManager.ControlsBound);
    }

    private sealed class TestPage : ContentPageBase<TestCellViewModel>
    {
        public TestPage()
        {
            this.InitializeStellarComponent(new TestCellViewModel());
        }

        public override void SetupUserInterface()
        {
        }

        public override void Bind(WeakCompositeDisposable disposables)
        {
        }
    }

    private sealed class TestViewCell : ViewCellBase<TestCellViewModel>
    {
        public TestViewCell()
        {
            this.InitializeStellarComponent(new TestCellViewModel());
        }

        public override void SetupUserInterface()
        {
        }

        public override void Bind(WeakCompositeDisposable disposables)
        {
        }
    }
}

#pragma warning restore CS0618
