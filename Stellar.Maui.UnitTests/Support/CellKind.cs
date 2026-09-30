namespace Stellar.Maui.UnitTests.Support;

/// <summary>
/// The Stellar.Maui base views built on MauiViewManager that can be used as an item view.
/// The data-model kinds keep one view model per view and map each row onto it; the others
/// are handed a view model per row.
/// </summary>
public enum CellKind
{
    GridWithDataModel,
    Grid,
    ContentViewWithDataModel,
    ContentView,
    StackLayoutWithDataModel,
    StackLayout,
}
