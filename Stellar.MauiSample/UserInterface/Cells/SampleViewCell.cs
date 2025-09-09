namespace Stellar.MauiSample.UserInterface.Cells;

public class SampleViewCell : ViewCellBase<ViewModels.TestItem>
{
    private Label _name;

    public SampleViewCell()
    {
        this.InitializeStellarComponent();
    }

    public override void SetupUserInterface()
    {
        View =
            new Label
            {
            }
                .Assign(out _name);
    }

    public override void Bind(WeakCompositeDisposable disposables)
    {
        this.OneWayBind(ViewModel, static vm => vm.Value1, static ui => ui._name.Text)
            .DisposeWith(disposables);
    }
}
