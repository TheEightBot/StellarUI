namespace Stellar.Maui.UnitTests.Support;

internal interface ITestCell : IStellarView<TestCellViewModel>
{
    int BindCalls { get; }

    Label Name { get; }

    Label Amount { get; }

    Button Select { get; }

    List<LifecycleEvent> Events { get; }
}
