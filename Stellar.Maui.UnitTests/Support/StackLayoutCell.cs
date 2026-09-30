using Stellar.Maui.Views;

namespace Stellar.Maui.UnitTests.Support;

internal sealed class StackLayoutCell : StackLayoutBase<TestCellViewModel>, ITestCell
{
    public StackLayoutCell(bool maintain = false)
    {
        ViewManager.LifecycleEvents.Subscribe(Events.Add);

        this.InitializeStellarComponent(null, maintain);
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
}
