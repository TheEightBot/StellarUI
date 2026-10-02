using Stellar.Maui.Views;

namespace Stellar.Maui.UnitTests.Support;

internal sealed class CommandGridCell : GridBase<CommandCellViewModel>, ICommandCell
{
    public CommandGridCell(CommandCellViewModel viewModel)
    {
        this.InitializeStellarComponent(viewModel);
    }

    public Button Select { get; } = new();

    public override void SetupUserInterface() => Children.Add(Select);

    public override void Bind(WeakCompositeDisposable disposables)
    {
        this.BindCommand(ViewModel, static vm => vm.Select, static ui => ui.Select)
            .DisposeWith(disposables);
    }
}
