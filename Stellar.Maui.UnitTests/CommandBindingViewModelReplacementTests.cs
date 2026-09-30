using System.Reactive.Linq;

namespace Stellar.Maui.UnitTests;

public sealed class CommandBindingViewModelReplacementTests : StellarTestBase
{
    public static TheoryData<CommandCellKind> Kinds =>
        new()
        {
            CommandCellKind.Grid,
            CommandCellKind.ContentView,
            CommandCellKind.StackLayout,
        };

    [Theory]
    [MemberData(nameof(Kinds))]
    public void BindsTheCommandOfTheFirstViewModel(CommandCellKind kind)
    {
        var first = new CommandCellViewModel();
        var cell = CommandCells.Create(kind, first);

        Assert.Same(first.Select, cell.Select.Command);
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void ReplacingTheViewModelThroughTheBindingContextBindsTheNewCommand(CommandCellKind kind)
    {
        var first = new CommandCellViewModel();
        var cell = CommandCells.Create(kind, first);
        var second = new CommandCellViewModel();

        ((View)cell).BindingContext = second;

        Assert.Same(second.Select, cell.Select.Command);

        cell.Select.Command!.Execute(null);

        Assert.Equal(1, second.SelectCalls);
        Assert.Equal(0, first.SelectCalls);
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void ReplacingTheViewModelAgainDoesNotBringBackAnEarlierCommand(CommandCellKind kind)
    {
        var first = new CommandCellViewModel();
        var cell = CommandCells.Create(kind, first);
        var second = new CommandCellViewModel();
        var third = new CommandCellViewModel();

        ((View)cell).BindingContext = second;
        ((View)cell).BindingContext = third;

        Assert.Same(third.Select, cell.Select.Command);

        cell.Select.Command!.Execute(null);

        Assert.Equal(1, third.SelectCalls);
        Assert.Equal(0, first.SelectCalls);
        Assert.Equal(0, second.SelectCalls);
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void UnregisteringTheBindingsAfterReplacementsLeavesNoCommand(CommandCellKind kind)
    {
        var cell = CommandCells.Create(kind, new CommandCellViewModel());

        ((View)cell).BindingContext = new CommandCellViewModel();
        ((View)cell).BindingContext = new CommandCellViewModel();

        cell.ViewManager.UnregisterBindings(cell);

        Assert.Null(cell.Select.Command);
    }

    [Fact]
    public void TheCommandParameterKeepsFollowingTheViewAfterReplacement()
    {
        var first = new CommandCellViewModel();
        first.SetupViewModel();
        var view = new ParameterView { ViewModel = first };
        using var binding = view.BindCommand(view.ViewModel, static vm => vm.Select, static ui => ui.Select, view.WhenAnyValue(static x => x.Parameter));
        var second = new CommandCellViewModel();
        second.SetupViewModel();

        view.ViewModel = second;
        view.Parameter = "second";

        Assert.Same(second.Select, view.Select.Command);
        Assert.Equal("second", view.Select.CommandParameter);
    }

    [Fact]
    public void DisposingTheBindingAfterReplacementRestoresTheOriginalState()
    {
        var first = new CommandCellViewModel();
        first.SetupViewModel();
        var view = new ParameterView { ViewModel = first };
        var binding = view.BindCommand(view.ViewModel, static vm => vm.Select, static ui => ui.Select, view.WhenAnyValue(static x => x.Parameter));
        var second = new CommandCellViewModel();
        second.SetupViewModel();
        view.ViewModel = second;

        binding.Dispose();

        Assert.Null(view.Select.Command);
        Assert.Null(view.Select.CommandParameter);
    }
}
