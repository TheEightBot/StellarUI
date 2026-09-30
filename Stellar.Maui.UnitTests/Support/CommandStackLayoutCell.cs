using Stellar.Maui.Views;

namespace Stellar.Maui.UnitTests.Support;

internal sealed class CommandStackLayoutCell : StackLayoutBase<CommandCellViewModel>, ICommandCell
{
    public CommandStackLayoutCell(CommandCellViewModel viewModel)
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
