using Stellar.Maui.Views;

namespace Stellar.Maui.UnitTests.Support;

internal sealed class ContentViewDataModelCell : ContentViewBase<TestCellViewModel, TestItem>, ITestCell
{
    public ContentViewDataModelCell(LongLivedSource? source = null, bool maintain = false)
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
        Content = new VerticalStackLayout { Children = { Name, Amount, Select } };
    }

    public override void Bind(WeakCompositeDisposable disposables)
    {
        BindCalls++;

        TestCellBindings.Bind(this, disposables);
    }

    protected override void MapDataModelToViewModel(TestCellViewModel viewModel, TestItem dataModel) =>
        viewModel.Item = dataModel;
}
