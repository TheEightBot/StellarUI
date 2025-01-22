using Stellar.MauiSample.ViewModels;

namespace Stellar.MauiSample.UserInterface.Cells;

public class SampleMappedViewCell : ViewCellBase<ViewModels.WrappedTestItem, ViewModels.TestItem>
{
    private Label _name;

    public SampleMappedViewCell()
    {
        this.InitializeStellarComponent(new WrappedTestItem());
    }

    public override void SetupUserInterface()
    {
        View =
            new Label
            {
            }
                .Assign(out _name);
    }

    public override void Bind(CompositeDisposable disposables)
    {
        this.OneWayBind(ViewModel, static vm => vm.Item.Value1, static ui => ui._name.Text)
            .DisposeWith(disposables);
    }

    protected override void MapDataModelToViewModel(WrappedTestItem viewModel, TestItem dataModel)
    {
        viewModel.Item = dataModel;
    }
}
