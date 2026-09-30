namespace Stellar.Maui.UnitTests.Support;

internal static class TestCells
{
    public static ITestCell Create(CellKind kind, LongLivedSource? source = null, bool maintain = false) =>
        kind switch
        {
            CellKind.GridWithDataModel => new GridDataModelCell(source, maintain),
            CellKind.Grid => new GridCell(maintain),
            CellKind.ContentViewWithDataModel => new ContentViewDataModelCell(source, maintain),
            CellKind.ContentView => new ContentViewCell(maintain),
            CellKind.StackLayoutWithDataModel => new StackLayoutDataModelCell(source, maintain),
            CellKind.StackLayout => new StackLayoutCell(maintain),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

    /// <summary>
    /// What the list hands the cell as its binding context for a row: the row itself for a
    /// cell that maps a data model, a view model wrapping the row otherwise.
    /// </summary>
    public static object Row(CellKind kind, string name, int amount, LongLivedSource? source = null) =>
        kind switch
        {
            CellKind.GridWithDataModel or CellKind.ContentViewWithDataModel or CellKind.StackLayoutWithDataModel =>
                new TestItem(name, amount),
            _ => new TestCellViewModel(source) { Item = new TestItem(name, amount) },
        };
}
