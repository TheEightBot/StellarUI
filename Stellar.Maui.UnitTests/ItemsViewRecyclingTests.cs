namespace Stellar.Maui.UnitTests;

/// <summary>
/// An item view loses its window each time its list recycles it and gets it back when it
/// is rebound. A view that owns its view model and maps each row onto it keeps its bindings
/// through that, up to the number of views a recycler retains, and gives them up when the
/// list itself leaves the window.
/// </summary>
public class ItemsViewRecyclingTests : MauiTestBase
{
    [Theory]
    [MemberData(nameof(DataModelKindsOnEachPlatform))]
    public void Recycle_KeepsTheViewAndViewModelBindings(CellKind kind, FirstBind firstBind)
    {
        var source = new LongLivedSource();
        var list = new ListHarness();
        var cell = TestCells.Create(kind, source);
        list.BindNew(cell, TestCells.Row(kind, "Chair", 3), firstBind);

        list.Recycle(cell);

        Assert.True(cell.ViewManager.ControlsBound);
        Assert.True(cell.ViewModel!.BindingsRegistered);
        Assert.Equal(1, source.HandlerCount);
    }

    [Theory]
    [MemberData(nameof(DataModelKindsOnEachPlatform))]
    public void RecycleAndRebind_DoesNotBindAgain(CellKind kind, FirstBind firstBind)
    {
        var source = new LongLivedSource();
        var list = new ListHarness();
        var cell = TestCells.Create(kind, source);
        list.BindNew(cell, TestCells.Row(kind, "Row 0", 0), firstBind);
        var viewBinds = cell.BindCalls;
        var viewModelBinds = cell.ViewModel!.BindCalls;

        for (var row = 1; row <= 20; row++)
        {
            list.Recycle(cell);
            list.Bind(cell, TestCells.Row(kind, $"Row {row}", row));
        }

        Assert.Equal(viewBinds, cell.BindCalls);
        Assert.Equal(viewModelBinds, cell.ViewModel!.BindCalls);
        Assert.Equal(1, source.HandlerCount);
    }

    [Theory]
    [MemberData(nameof(DataModelKindsOnEachPlatform))]
    public void Rebind_UpdatesTheViewThroughTheBindingsItKept(CellKind kind, FirstBind firstBind)
    {
        var source = new LongLivedSource();
        var list = new ListHarness();
        var cell = TestCells.Create(kind, source);
        list.BindNew(cell, TestCells.Row(kind, "Chair", 3), firstBind);
        list.Recycle(cell);

        list.Bind(cell, TestCells.Row(kind, "Table", 7));

        Assert.Equal("Table", cell.Name.Text);
        Assert.Equal("7 ft", cell.Amount.Text);
    }

    [Theory]
    [MemberData(nameof(DataModelKindsOnEachPlatform))]
    public void Rebind_AChangeOnTheLongLivedSourceStillReachesTheView(CellKind kind, FirstBind firstBind)
    {
        var source = new LongLivedSource();
        var list = new ListHarness();
        var cell = TestCells.Create(kind, source);
        list.BindNew(cell, TestCells.Row(kind, "Chair", 3), firstBind);
        list.Recycle(cell);
        list.Bind(cell, TestCells.Row(kind, "Table", 7));

        source.Unit = "m";

        Assert.Equal("7 m", cell.Amount.Text);
    }

    [Theory]
    [MemberData(nameof(DataModelKindsOnEachPlatform))]
    public void Rebind_TheCommandStillReachesTheViewModel(CellKind kind, FirstBind firstBind)
    {
        var list = new ListHarness();
        var cell = TestCells.Create(kind);
        list.BindNew(cell, TestCells.Row(kind, "Chair", 3), firstBind);
        list.Recycle(cell);
        list.Bind(cell, TestCells.Row(kind, "Table", 7));

        cell.Select.Command.Execute(null);

        Assert.Equal(1, cell.ViewModel!.SelectCalls);
    }

    [Theory]
    [MemberData(nameof(DataModelKindsOnEachPlatform))]
    public void Recycle_RaisesDetachedButNotDeactivated(CellKind kind, FirstBind firstBind)
    {
        var list = new ListHarness();
        var cell = TestCells.Create(kind);
        list.BindNew(cell, TestCells.Row(kind, "Chair", 3), firstBind);
        cell.Events.Clear();
        cell.ViewModel!.Events.Clear();

        list.Recycle(cell);

        Assert.Equal(new[] { LifecycleEvent.Detached }, cell.Events);
        Assert.Equal(new[] { LifecycleEvent.Detached }, cell.ViewModel!.Events);
    }

    [Theory]
    [MemberData(nameof(DataModelKindsOnEachPlatform))]
    public void Rebind_RaisesAttachedButNotActivated(CellKind kind, FirstBind firstBind)
    {
        var list = new ListHarness();
        var cell = TestCells.Create(kind);
        list.BindNew(cell, TestCells.Row(kind, "Chair", 3), firstBind);
        list.Recycle(cell);
        cell.Events.Clear();
        cell.ViewModel!.Events.Clear();

        list.Bind(cell, TestCells.Row(kind, "Table", 7));

        Assert.Equal(new[] { LifecycleEvent.Attached }, cell.Events);
        Assert.Equal(new[] { LifecycleEvent.Attached }, cell.ViewModel!.Events);
    }

    [Theory]
    [MemberData(nameof(CellKinds))]
    public void NewView_BoundTheWayAndroidBinds_LosesItsWindowOnceBeforeItHasAParentAsBefore(CellKind kind)
    {
        // Nothing can be kept here: the view has never had a parent when its window goes,
        // so there is no list to say it belongs to. It costs each view one extra bind when
        // it is created, as it did before, and nothing per row.
        var list = new ListHarness();
        var cell = TestCells.Create(kind);
        var created = cell.BindCalls;
        cell.Events.Clear();

        list.BindNew(cell, TestCells.Row(kind, "Chair", 3), FirstBind.PropagateWindowThenAddToList);

        Assert.Equal(
            new[]
            {
                LifecycleEvent.Activated,
                LifecycleEvent.Attached,
                LifecycleEvent.Detached,
                LifecycleEvent.Deactivated,
                LifecycleEvent.Initialized,
                LifecycleEvent.Activated,
                LifecycleEvent.Attached,
            },
            cell.Events);
        Assert.Equal(created + 1, cell.BindCalls);
        Assert.True(cell.ViewManager.ControlsBound);
    }

    [Theory]
    [MemberData(nameof(ViewModelPerRowKinds))]
    public void ViewGivenAViewModelPerRow_IsTornDownOnRecycleAndBoundAgainOnRebindAsBefore(CellKind kind)
    {
        // Keeping these bindings would mean carrying them from one view model to the next.
        // Bindings written against the view model a view had when it was bound would go
        // stale, and ReactiveUI 24's BindCommand leaves the control without a command when
        // the view model under a live binding is replaced.
        var source = new LongLivedSource();
        var list = new ListHarness();
        var cell = TestCells.Create(kind);
        var first = new TestCellViewModel(source) { Item = new TestItem("Chair", 3) };
        var second = new TestCellViewModel(source) { Item = new TestItem("Table", 7) };
        list.BindNew(cell, first);
        var binds = cell.BindCalls;
        cell.Events.Clear();

        list.Recycle(cell);

        Assert.Equal(new[] { LifecycleEvent.Detached, LifecycleEvent.Deactivated }, cell.Events);
        Assert.Equal(new[] { LifecycleEvent.Detached, LifecycleEvent.Deactivated }, first.Events.TakeLast(2));
        Assert.False(cell.ViewManager.ControlsBound);
        Assert.False(first.BindingsRegistered);
        Assert.Equal(0, source.HandlerCount);

        cell.Events.Clear();
        list.Bind(cell, second);

        Assert.Equal(
            new[] { LifecycleEvent.Initialized, LifecycleEvent.Activated, LifecycleEvent.Attached },
            cell.Events);
        Assert.Equal(binds + 1, cell.BindCalls);
        Assert.True(second.BindingsRegistered);
        Assert.False(first.BindingsRegistered);
        Assert.Equal("Table", cell.Name.Text);
        Assert.Equal("7 ft", cell.Amount.Text);

        cell.Select.Command.Execute(null);

        Assert.Equal(1, second.SelectCalls);
    }

    [Fact]
    public void ViewGivenAViewModelPerRow_DoesNotTakeASlot()
    {
        var source = new LongLivedSource();
        var list = new ListHarness();
        var perRow = new GridCell();
        list.BindNew(perRow, new TestCellViewModel(source) { Item = new TestItem("Chair", 3) });
        var cells = BindCells(list, source, Cap);

        list.Recycle(perRow);

        foreach (var cell in cells)
        {
            list.Recycle(cell);
        }

        Assert.All(cells, static cell => Assert.True(cell.ViewManager.ControlsBound));
    }

    [Fact]
    public void ViewThatMapsADataModel_GivenAViewModelAsItsRow_IsTornDownOnRecycleAsBefore()
    {
        var list = new ListHarness();
        var cell = new GridDataModelCell();
        list.BindNew(cell, new TestCellViewModel { Item = new TestItem("Chair", 3) });
        cell.Events.Clear();

        list.Recycle(cell);

        Assert.Equal(new[] { LifecycleEvent.Detached, LifecycleEvent.Deactivated }, cell.Events);
        Assert.False(cell.ViewManager.ControlsBound);
    }

    [Fact]
    public void ViewWithoutAViewModel_IsTornDownOnRecycleAsBefore()
    {
        var list = new ListHarness();
        var cell = new GridCell();
        list.BindNew(cell, new TestItem("Chair", 3));
        cell.Events.Clear();

        list.Recycle(cell);

        Assert.Null(cell.ViewModel);
        Assert.Equal(new[] { LifecycleEvent.Detached, LifecycleEvent.Deactivated }, cell.Events);
        Assert.False(cell.ViewManager.ControlsBound);
    }

    [Fact]
    public void RecycledView_WhoseViewModelWasReplacedMeanwhile_IsBoundAgainWhenItReturns()
    {
        var source = new LongLivedSource();
        var list = new ListHarness();
        var cell = new GridDataModelCell(source);
        list.BindNew(cell, new TestItem("Chair", 3));
        var original = cell.ViewModel!;
        var replacement = new TestCellViewModel(source);
        var binds = cell.BindCalls;
        list.Recycle(cell);
        cell.ViewModel = replacement;
        cell.Events.Clear();

        list.Bind(cell, new TestItem("Table", 7));

        Assert.Equal(
            new[]
            {
                LifecycleEvent.Deactivated,
                LifecycleEvent.Initialized,
                LifecycleEvent.Activated,
                LifecycleEvent.Attached,
            },
            cell.Events);
        Assert.Equal(binds + 1, cell.BindCalls);
        Assert.False(original.BindingsRegistered);
        Assert.True(replacement.BindingsRegistered);
        Assert.Equal("Table", cell.Name.Text);
        Assert.Equal(1, source.HandlerCount);

        cell.Select.Command.Execute(null);

        Assert.Equal(1, replacement.SelectCalls);
    }

    [Fact]
    public void RecycledView_WhoseViewModelWasReplacedMeanwhile_LetsGoOfBothWhenTheListLeavesTheWindow()
    {
        var source = new LongLivedSource();
        var list = new ListHarness();
        var cell = new GridDataModelCell(source);
        list.BindNew(cell, new TestItem("Chair", 3));
        var original = cell.ViewModel!;
        var replacement = new TestCellViewModel(source);
        list.Recycle(cell);
        cell.ViewModel = replacement;

        list.RemoveFromWindow();

        Assert.False(cell.ViewManager.ControlsBound);
        Assert.False(original.BindingsRegistered);
        Assert.False(replacement.BindingsRegistered);
        Assert.Equal(0, source.HandlerCount);
    }

    [Fact]
    public void RecyclingMoreViewsThanARecyclerRetains_TearsDownTheRestImmediately()
    {
        var source = new LongLivedSource();
        var list = new ListHarness();
        var cells = BindCells(list, source, 22);

        foreach (var cell in cells)
        {
            list.Recycle(cell);
        }

        Assert.All(cells.Take(Cap), static cell => Assert.True(cell.ViewManager.ControlsBound));
        Assert.All(cells.Skip(Cap), static cell => Assert.False(cell.ViewManager.ControlsBound));
        Assert.All(cells.Skip(Cap), static cell => Assert.Contains(LifecycleEvent.Deactivated, cell.Events));
        Assert.Equal(Cap, source.HandlerCount);
    }

    [Fact]
    public void RecycledItemViewLimit_RaisedToMatchALargerPool_KeepsThatManyViews()
    {
        var source = new LongLivedSource();
        var list = new ListHarness();
        list.List.RecycledItemViewLimit(24);
        var cells = BindCells(list, source, 30);

        foreach (var cell in cells)
        {
            list.Recycle(cell);
        }

        Assert.All(cells.Take(24), static cell => Assert.True(cell.ViewManager.ControlsBound));
        Assert.All(cells.Skip(24), static cell => Assert.False(cell.ViewManager.ControlsBound));

        list.RemoveFromWindow();

        Assert.Equal(0, source.HandlerCount);
    }

    [Fact]
    public void ListThatHasNotOptedIn_TearsEveryViewDownAsBefore()
    {
        var source = new LongLivedSource();
        var list = new ListHarness(recycledItemViewLimit: null);
        var cells = BindCells(list, source, 2);

        foreach (var cell in cells)
        {
            list.Recycle(cell);
        }

        Assert.All(cells, static cell => Assert.False(cell.ViewManager.ControlsBound));
        Assert.Equal(0, source.HandlerCount);
    }

    [Fact]
    public void RecycledItemViewLimit_Lowered_ReleasesTheViewsAlreadyKept()
    {
        var source = new LongLivedSource();
        var list = new ListHarness();
        var cells = BindCells(list, source, 2);

        foreach (var cell in cells)
        {
            list.Recycle(cell);
        }

        list.List.RecycledItemViewLimit(0);

        Assert.All(cells, static cell => Assert.False(cell.ViewManager.ControlsBound));
        Assert.Equal(0, source.HandlerCount);
    }

    [Fact]
    public void RecycledItemViewLimit_OfZero_TearsEveryViewDownAsBefore()
    {
        var source = new LongLivedSource();
        var list = new ListHarness();
        list.List.RecycledItemViewLimit(0);
        var cells = BindCells(list, source, 2);

        foreach (var cell in cells)
        {
            list.Recycle(cell);
        }

        Assert.All(cells, static cell => Assert.False(cell.ViewManager.ControlsBound));
        Assert.Equal(0, source.HandlerCount);
    }

    [Fact]
    public void RecycledItemViewLimit_AppliesToThatListOnly()
    {
        var source = new LongLivedSource();
        var raised = new ListHarness();
        var other = new ListHarness();
        var returned = raised.List.RecycledItemViewLimit(Cap + 1);
        var cells = BindCells(other, source, Cap + 1);

        foreach (var cell in cells)
        {
            other.Recycle(cell);
        }

        Assert.Same(raised.List, returned);
        Assert.False(cells[Cap].ViewManager.ControlsBound);
    }

    [Fact]
    public void RecycledItemViewLimit_RejectsANegativeNumber()
    {
        var list = new ListHarness();

        Assert.Throws<ArgumentOutOfRangeException>(() => list.List.RecycledItemViewLimit(-1));
    }

    [Fact]
    public void RebindingTheRetainedViews_DoesNotBindThemAgainAndFreesTheirSlots()
    {
        var source = new LongLivedSource();
        var list = new ListHarness();
        var cells = BindCells(list, source, Cap * 2);
        var first = cells.Take(Cap).ToList();
        var second = cells.Skip(Cap).ToList();

        foreach (var cell in first)
        {
            list.Recycle(cell);
        }

        foreach (var cell in first)
        {
            list.Bind(cell, new TestItem("Rebound", 1));
        }

        foreach (var cell in second)
        {
            list.Recycle(cell);
        }

        Assert.All(first, static cell => Assert.Equal(1, cell.BindCalls));
        Assert.All(second, static cell => Assert.True(cell.ViewManager.ControlsBound));
        Assert.Equal(Cap * 2, source.HandlerCount);
    }

    [Fact]
    public void ViewTornDownBeyondTheCap_ActivatesNormallyWhenRebound()
    {
        var source = new LongLivedSource();
        var list = new ListHarness();
        var cells = BindCells(list, source, Cap + 1);

        foreach (var cell in cells)
        {
            list.Recycle(cell);
        }

        var beyond = cells[Cap];
        beyond.Events.Clear();

        list.Bind(beyond, new TestItem("Table", 7));

        Assert.Equal(
            new[] { LifecycleEvent.Initialized, LifecycleEvent.Activated, LifecycleEvent.Attached },
            beyond.Events);
        Assert.Equal(2, beyond.BindCalls);
        Assert.Equal("Table", beyond.Name.Text);
        Assert.Equal("7 ft", beyond.Amount.Text);
    }

    [Theory]
    [MemberData(nameof(DataModelKindsOnEachPlatform))]
    public void ListLeavingTheWindow_DeactivatesTheViewsItRecycled(CellKind kind, FirstBind firstBind)
    {
        var source = new LongLivedSource();
        var list = new ListHarness();
        var cell = TestCells.Create(kind, source);
        list.BindNew(cell, TestCells.Row(kind, "Chair", 3), firstBind);
        list.Recycle(cell);
        cell.Events.Clear();

        list.RemoveFromWindow();

        Assert.Equal(new[] { LifecycleEvent.Deactivated }, cell.Events);
        Assert.False(cell.ViewManager.ControlsBound);
        Assert.False(cell.ViewModel!.BindingsRegistered);
        Assert.Equal(0, source.HandlerCount);
    }

    [Theory]
    [MemberData(nameof(CellKindsOnEachPlatform))]
    public void ListLeavingTheWindow_DeactivatesTheViewsStillBoundToARow(CellKind kind, FirstBind firstBind)
    {
        var source = new LongLivedSource();
        var list = new ListHarness();
        var cell = TestCells.Create(kind, source);
        list.BindNew(cell, TestCells.Row(kind, "Chair", 3, source), firstBind);
        var viewModel = cell.ViewModel!;
        cell.Events.Clear();

        list.RemoveFromWindow();

        Assert.Equal(new[] { LifecycleEvent.Detached, LifecycleEvent.Deactivated }, cell.Events);
        Assert.False(cell.ViewManager.ControlsBound);
        Assert.False(viewModel.BindingsRegistered);
        Assert.Equal(0, source.HandlerCount);
    }

    [Fact]
    public void ListLeavingTheWindow_DisposesARecycledViewThenAndNotBefore()
    {
        var list = new ListHarness();
        var cell = new DisposableCell();
        list.BindNew(cell, new TestItem("Chair", 3));

        list.Recycle(cell);

        Assert.Equal(0, cell.Disposal.DisposeCount);

        list.RemoveFromWindow();

        Assert.Equal(1, cell.Disposal.DisposeCount);
    }

    [Theory]
    [MemberData(nameof(DataModelKindsOnEachPlatform))]
    public void ListReturningToTheWindow_ARecycledViewActivatesNormallyWhenRebound(CellKind kind, FirstBind firstBind)
    {
        var source = new LongLivedSource();
        var list = new ListHarness();
        var cell = TestCells.Create(kind, source);
        list.BindNew(cell, TestCells.Row(kind, "Chair", 3), firstBind);
        var binds = cell.BindCalls;
        list.Recycle(cell);
        list.RemoveFromWindow();
        list.ReturnToWindow();
        cell.Events.Clear();

        list.Bind(cell, TestCells.Row(kind, "Table", 7));

        Assert.Equal(
            new[] { LifecycleEvent.Initialized, LifecycleEvent.Activated, LifecycleEvent.Attached },
            cell.Events);
        Assert.Equal(binds + 1, cell.BindCalls);
        Assert.True(cell.ViewModel!.BindingsRegistered);
        Assert.Equal("Table", cell.Name.Text);
        Assert.Equal("7 ft", cell.Amount.Text);
        Assert.Equal(1, source.HandlerCount);

        list.Recycle(cell);

        Assert.True(cell.ViewManager.ControlsBound);
    }

    [Theory]
    [MemberData(nameof(DataModelKindsOnEachPlatform))]
    public void ListReturningToTheWindow_AViewStillBoundToARowActivatesWithIt(CellKind kind, FirstBind firstBind)
    {
        var source = new LongLivedSource();
        var list = new ListHarness();
        var cell = TestCells.Create(kind, source);
        list.BindNew(cell, TestCells.Row(kind, "Chair", 3), firstBind);
        list.RemoveFromWindow();
        cell.Events.Clear();

        list.ReturnToWindow();

        Assert.Equal(
            new[] { LifecycleEvent.Initialized, LifecycleEvent.Activated, LifecycleEvent.Attached },
            cell.Events);
        Assert.Equal("3 ft", cell.Amount.Text);

        list.Recycle(cell);

        Assert.True(cell.ViewManager.ControlsBound);
    }

    [Fact]
    public void RecycleWhileTheListIsOutOfTheWindow_LeavesNothingParked()
    {
        var source = new LongLivedSource();
        var list = new ListHarness();
        var cells = BindCells(list, source, 2);
        list.RemoveFromWindow();

        list.Recycle(cells[0]);
        list.ReturnToWindow();

        Assert.False(cells[0].ViewManager.ControlsBound);
        Assert.True(cells[1].ViewManager.ControlsBound);
        Assert.Equal(1, source.HandlerCount);
    }

    [Theory]
    [InlineData(FirstBind.AddToList)]
    [InlineData(FirstBind.PropagateWindowThenAddToList)]
    public void NewView_ReleasesTheViewsTheRecyclerMustHaveDropped(FirstBind firstBind)
    {
        // A recycler only builds a view when it has none pooled to reuse, so one arriving
        // while views are parked means the pool was emptied behind them. MAUI does that on
        // Android whenever ItemsSource or ItemTemplate changes and whenever the list swaps
        // to or from its empty view, and the adapter is not told.
        var source = new LongLivedSource();
        var list = new ListHarness();
        var dropped = BindCells(list, source, 3);

        foreach (var cell in dropped)
        {
            list.Recycle(cell);
        }

        var created = new GridDataModelCell(source);
        list.BindNew(created, new TestItem("Table", 7), firstBind);

        Assert.All(dropped, static cell => Assert.False(cell.ViewManager.ControlsBound));
        Assert.All(dropped, static cell => Assert.Contains(LifecycleEvent.Deactivated, cell.Events));
        Assert.True(created.ViewManager.ControlsBound);
        Assert.Equal(1, source.HandlerCount);
    }

    [Theory]
    [InlineData(FirstBind.AddToList)]
    [InlineData(FirstBind.PropagateWindowThenAddToList)]
    public void ViewsTheRecyclerDropped_DoNotUseUpTheCapForTheViewsThatReplaceThem(FirstBind firstBind)
    {
        var source = new LongLivedSource();
        var list = new ListHarness();

        foreach (var cell in BindCells(list, source, Cap, firstBind))
        {
            list.Recycle(cell);
        }

        var replacements = BindCells(list, source, Cap, firstBind);

        foreach (var cell in replacements)
        {
            list.Recycle(cell);
        }

        Assert.All(replacements, static cell => Assert.True(cell.ViewManager.ControlsBound));
        Assert.Equal(Cap, source.HandlerCount);
    }

    [Fact]
    public void ViewReboundWhileRecycledViewsAreBeingReleased_KeepsItsBindings()
    {
        var source = new LongLivedSource();
        var list = new ListHarness();
        var cells = BindCells(list, source, 2);
        list.Recycle(cells[0]);
        list.Recycle(cells[1]);
        using var rebind =
            cells[0].ViewManager.Deactivated
                .Subscribe(_ => list.Bind(cells[1], new TestItem("Table", 7)));

        list.BindNew(new GridDataModelCell(source), new TestItem("Desk", 1));

        Assert.False(cells[0].ViewManager.ControlsBound);
        Assert.True(cells[1].ViewManager.ControlsBound);
        Assert.Equal(1, cells[1].BindCalls);
        Assert.Equal("Table", cells[1].Name.Text);
    }

    [Theory]
    [MemberData(nameof(DataModelKindsOnEachPlatform))]
    public void RecycledView_AddedToAParentThatIsNotAList_AttachesWithItsBindingsAndIsTornDownWhenRemoved(CellKind kind, FirstBind firstBind)
    {
        var source = new LongLivedSource();
        var list = new ListHarness();
        var cell = TestCells.Create(kind, source);
        list.BindNew(cell, TestCells.Row(kind, "Chair", 3), firstBind);
        var binds = cell.BindCalls;
        list.Recycle(cell);
        cell.Events.Clear();

        list.Side.Add((View)cell);

        Assert.Equal(new[] { LifecycleEvent.Attached }, cell.Events);
        Assert.Equal(binds, cell.BindCalls);

        cell.Events.Clear();
        list.Side.Remove((View)cell);

        Assert.Equal(new[] { LifecycleEvent.Detached, LifecycleEvent.Deactivated }, cell.Events);
        Assert.False(cell.ViewManager.ControlsBound);
        Assert.Equal(0, source.HandlerCount);
    }

    [Fact]
    public void RecycledView_AddedToAParentThatIsNotAList_GivesUpItsSlot()
    {
        var source = new LongLivedSource();
        var list = new ListHarness();
        var cells = BindCells(list, source, Cap + 1);
        var moved = cells[0];
        var others = cells.Skip(1).ToList();
        list.Recycle(moved);

        list.Side.Add(moved);

        foreach (var cell in others)
        {
            list.Recycle(cell);
        }

        Assert.All(others, static cell => Assert.True(cell.ViewManager.ControlsBound));

        list.RemoveFromWindow();

        Assert.Equal(0, source.HandlerCount);
    }

    [Theory]
    [MemberData(nameof(DataModelKindsOnEachPlatform))]
    public void BoundView_MovedToAParentThatIsNotAList_IsTornDownWhenRemoved(CellKind kind, FirstBind firstBind)
    {
        // The window does not change when a view moves within it, so only the change of
        // parent says the view no longer belongs to the list.
        var source = new LongLivedSource();
        var list = new ListHarness();
        var cell = TestCells.Create(kind, source);
        list.BindNew(cell, TestCells.Row(kind, "Chair", 3), firstBind);
        list.Side.Add((View)cell);
        cell.Events.Clear();

        list.Side.Remove((View)cell);

        Assert.Equal(new[] { LifecycleEvent.Detached, LifecycleEvent.Deactivated }, cell.Events);
        Assert.False(cell.ViewManager.ControlsBound);
        Assert.Equal(0, source.HandlerCount);
    }

    [Theory]
    [MemberData(nameof(CellKinds))]
    public void MaintainedView_IsDeactivatedAndReactivatedOnRecycleAsBefore(CellKind kind)
    {
        var source = new LongLivedSource();
        var list = new ListHarness();
        var cell = TestCells.Create(kind, source, maintain: true);
        var row = TestCells.Row(kind, "Chair", 3, source);
        list.BindNew(cell, row);
        cell.Events.Clear();

        list.Recycle(cell);

        Assert.Equal(new[] { LifecycleEvent.Detached, LifecycleEvent.Deactivated }, cell.Events);
        Assert.True(cell.ViewManager.ControlsBound);

        cell.Events.Clear();
        list.Bind(cell, row);

        Assert.Equal(new[] { LifecycleEvent.Activated, LifecycleEvent.Attached }, cell.Events);
        Assert.Equal(1, cell.BindCalls);
    }

    [Fact]
    public void MaintainedView_DoesNotTakeASlot()
    {
        var source = new LongLivedSource();
        var list = new ListHarness();
        var maintained = new GridDataModelCell(source, maintain: true);
        list.BindNew(maintained, new TestItem("Kept", 0));
        var cells = BindCells(list, source, Cap);

        list.Recycle(maintained);

        foreach (var cell in cells)
        {
            list.Recycle(cell);
        }

        Assert.All(cells, static cell => Assert.True(cell.ViewManager.ControlsBound));
    }

    [Fact]
    public void DisposingTheViewManagerOfARecycledView_GivesUpItsSlotAndIsSkippedWhenTheListLeaves()
    {
        var source = new LongLivedSource();
        var list = new ListHarness();
        var cells = BindCells(list, source, Cap + 1);
        var disposed = cells[0];
        var others = cells.Skip(1).ToList();
        list.Recycle(disposed);

        disposed.ViewManager.Dispose();

        foreach (var cell in others)
        {
            list.Recycle(cell);
        }

        Assert.All(others, static cell => Assert.True(cell.ViewManager.ControlsBound));

        list.RemoveFromWindow();

        Assert.All(others, static cell => Assert.False(cell.ViewManager.ControlsBound));
    }

    [Fact]
    public void TwoLists_KeepTheirRecycledViewsSeparately()
    {
        var source = new LongLivedSource();
        var first = new ListHarness();
        var second = new ListHarness();
        var firstCells = BindCells(first, source, Cap);
        var secondCells = BindCells(second, source, Cap);

        foreach (var cell in firstCells)
        {
            first.Recycle(cell);
        }

        foreach (var cell in secondCells)
        {
            second.Recycle(cell);
        }

        Assert.All(secondCells, static cell => Assert.True(cell.ViewManager.ControlsBound));

        first.RemoveFromWindow();

        Assert.All(firstCells, static cell => Assert.False(cell.ViewManager.ControlsBound));
        Assert.All(secondCells, static cell => Assert.True(cell.ViewManager.ControlsBound));
    }

    [Fact]
    public void RecycledView_ReturningToADifferentList_IsCountedThereFromThenOn()
    {
        var source = new LongLivedSource();
        var first = new ListHarness();
        var second = new ListHarness();
        var cell = new GridDataModelCell(source);
        first.BindNew(cell, new TestItem("Chair", 3));
        first.Recycle(cell);

        second.Bind(cell, new TestItem("Table", 7));
        second.Recycle(cell);
        first.RemoveFromWindow();

        Assert.True(cell.ViewManager.ControlsBound);

        second.RemoveFromWindow();

        Assert.False(cell.ViewManager.ControlsBound);
        Assert.Equal(0, source.HandlerCount);
    }

    private static List<GridDataModelCell> BindCells(
        ListHarness list,
        LongLivedSource source,
        int count,
        FirstBind firstBind = FirstBind.AddToList)
    {
        var cells = new List<GridDataModelCell>(count);

        for (var i = 0; i < count; i++)
        {
            var cell = new GridDataModelCell(source);
            list.BindNew(cell, new TestItem($"Row {i}", i), firstBind);
            cells.Add(cell);
        }

        return cells;
    }
}
