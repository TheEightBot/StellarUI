namespace Stellar.Maui.UnitTests.Support;

internal static class CommandCells
{
    public static ICommandCell Create(CommandCellKind kind, CommandCellViewModel viewModel) =>
        kind switch
        {
            CommandCellKind.Grid => new CommandGridCell(viewModel),
            CommandCellKind.ContentView => new CommandContentViewCell(viewModel),
            CommandCellKind.StackLayout => new CommandStackLayoutCell(viewModel),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
}
