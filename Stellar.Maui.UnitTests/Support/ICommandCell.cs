namespace Stellar.Maui.UnitTests.Support;

internal interface ICommandCell : IStellarView<CommandCellViewModel>
{
    Button Select { get; }
}
