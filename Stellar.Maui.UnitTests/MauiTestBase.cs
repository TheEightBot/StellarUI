namespace Stellar.Maui.UnitTests;

public abstract class MauiTestBase : StellarTestBase
{
    // The limit ListHarness opts its list in with, which is
    // RecyclerView.RecycledViewPool.DEFAULT_MAX_SCRAP.
    protected const int Cap = 5;

    public static TheoryData<CellKind> CellKinds =>
        new()
        {
            CellKind.GridWithDataModel,
            CellKind.Grid,
            CellKind.ContentViewWithDataModel,
            CellKind.ContentView,
            CellKind.StackLayoutWithDataModel,
            CellKind.StackLayout,
        };

    public static TheoryData<CellKind> DataModelKinds =>
        new()
        {
            CellKind.GridWithDataModel,
            CellKind.ContentViewWithDataModel,
            CellKind.StackLayoutWithDataModel,
        };

    public static TheoryData<CellKind> ViewModelPerRowKinds =>
        new()
        {
            CellKind.Grid,
            CellKind.ContentView,
            CellKind.StackLayout,
        };

    public static TheoryData<CellKind, FirstBind> DataModelKindsOnEachPlatform =>
        new()
        {
            { CellKind.GridWithDataModel, FirstBind.AddToList },
            { CellKind.GridWithDataModel, FirstBind.PropagateWindowThenAddToList },
            { CellKind.ContentViewWithDataModel, FirstBind.AddToList },
            { CellKind.ContentViewWithDataModel, FirstBind.PropagateWindowThenAddToList },
            { CellKind.StackLayoutWithDataModel, FirstBind.AddToList },
            { CellKind.StackLayoutWithDataModel, FirstBind.PropagateWindowThenAddToList },
        };

    public static TheoryData<CellKind, FirstBind> CellKindsOnEachPlatform =>
        new()
        {
            { CellKind.GridWithDataModel, FirstBind.AddToList },
            { CellKind.GridWithDataModel, FirstBind.PropagateWindowThenAddToList },
            { CellKind.Grid, FirstBind.AddToList },
            { CellKind.Grid, FirstBind.PropagateWindowThenAddToList },
            { CellKind.ContentViewWithDataModel, FirstBind.AddToList },
            { CellKind.ContentViewWithDataModel, FirstBind.PropagateWindowThenAddToList },
            { CellKind.ContentView, FirstBind.AddToList },
            { CellKind.ContentView, FirstBind.PropagateWindowThenAddToList },
            { CellKind.StackLayoutWithDataModel, FirstBind.AddToList },
            { CellKind.StackLayoutWithDataModel, FirstBind.PropagateWindowThenAddToList },
            { CellKind.StackLayout, FirstBind.AddToList },
            { CellKind.StackLayout, FirstBind.PropagateWindowThenAddToList },
        };
}
