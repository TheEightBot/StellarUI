using Stellar.Maui.Views;

namespace Stellar.Maui.UnitTests.Support;

internal sealed class StackLayoutDataModelCell : StackLayoutBase<TestCellViewModel, TestItem>, ITestCell
{
    public StackLayoutDataModelCell(LongLivedSource? source = null, bool maintain = false)
    {
        ViewManager.LifecycleEvents.Subscribe(Events.Add);

        this.InitializeStellarComponent(new TestCellViewModel(source), maintain);
    }

    public int BindCalls { get; private set; }

    public Label Name { get; } = new();

    public Label Amount { get; } = new();

    public Button Select { get; } = new();

    public List<LifecycleEvent> Events { get; } = new();

    public override void SetupUserInterface()
    {
        Children.Add(Name);
        Children.Add(Amount);
        Children.Add(Select);
    }

    public override void Bind(WeakCompositeDisposable disposables)
    {
        BindCalls++;

        TestCellBindings.Bind(this, disposables);
    }

    protected override void MapDataModelToViewModel(TestCellViewModel viewModel, TestItem dataModel) =>
        viewModel.Item = dataModel;
}
