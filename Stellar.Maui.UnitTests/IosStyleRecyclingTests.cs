namespace Stellar.Maui.UnitTests;

/// <summary>
/// iOS recycles differently from Android: TemplatedCell2.Unbind clears the view's binding
/// context before it removes the view from the list, where Android only removes it.
/// </summary>
public class IosStyleRecyclingTests : MauiTestBase
{
    [Theory]
    [MemberData(nameof(DataModelKinds))]
    public void RecycleThatClearsTheBindingContextFirst_KeepsTheBindingsAndRebindsThroughThem(CellKind kind)
    {
        var source = new LongLivedSource();
        var list = new ListHarness();
        var cell = TestCells.Create(kind, source);
        list.BindNew(cell, TestCells.Row(kind, "Chair", 3));
        var viewBinds = cell.BindCalls;
        var viewModel = cell.ViewModel;

        ((View)cell).BindingContext = null;
        list.Recycle(cell);

        Assert.Same(viewModel, cell.ViewModel);
        Assert.True(cell.ViewManager.ControlsBound);
        Assert.Equal(1, source.HandlerCount);

        list.Bind(cell, TestCells.Row(kind, "Table", 7));

        Assert.Equal(viewBinds, cell.BindCalls);
        Assert.Equal("Table", cell.Name.Text);
        Assert.Equal("7 ft", cell.Amount.Text);

        list.RemoveFromWindow();

        Assert.Equal(0, source.HandlerCount);
    }
}
