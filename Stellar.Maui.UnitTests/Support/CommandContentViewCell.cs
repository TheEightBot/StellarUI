using Stellar.Maui.Views;

namespace Stellar.Maui.UnitTests.Support;

internal sealed class CommandContentViewCell : ContentViewBase<CommandCellViewModel>, ICommandCell
{
    public CommandContentViewCell(CommandCellViewModel viewModel)
    {
        this.InitializeStellarComponent(viewModel);
    }

    public Button Select { get; } = new();

    public override void SetupUserInterface() => Content = Select;

    public override void Bind(WeakCompositeDisposable disposables)
    {
        this.BindCommand(ViewModel, static vm => vm.Select, static ui => ui.Select)
            .DisposeWith(disposables);
    }
}
