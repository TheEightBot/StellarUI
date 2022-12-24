namespace Stellar.MauiSample.UserInterface.Cells;

public class SampleViewCell : ViewCellBase<ViewModels.TestItem>
{
    private Label _name;

    public SampleViewCell()
    {
        this.InitializeComponent();
    }

    public override void SetupUserInterface()
    {
        View =
            new Label
            {
            }
                .Assign(out _name);
    }

    public override void BindControls()
    {
        this.OneWayBind(ViewModel, vm => vm.Value1, ui => ui._name.Text)
            .DisposeWith(ControlBindings);
    }
}
