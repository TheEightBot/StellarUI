using Stellar.Maui.Views;
using Stellar.UnitTests;

namespace Stellar.Maui.UnitTests.Support;

internal sealed class DisposableCell : GridBase<TestCellViewModel, TestItem>, ITestCell, IDisposable
{
    public DisposableCell(LongLivedSource? source = null)
    {
        ViewManager.LifecycleEvents.Subscribe(Events.Add);

        this.InitializeStellarComponent(new TestCellViewModel(source));
    }

    public int BindCalls { get; private set; }

    public Label Name { get; } = new();

    public Label Amount { get; } = new();

    public Button Select { get; } = new();

    public List<LifecycleEvent> Events { get; } = new();

    public TrackingDisposable Disposal { get; } = new();

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

    public void Dispose() => Disposal.Dispose();

    protected override void MapDataModelToViewModel(TestCellViewModel viewModel, TestItem dataModel) =>
        viewModel.Item = dataModel;
}
