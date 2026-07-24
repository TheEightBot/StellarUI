using System.Reactive;
using Stellar.ViewModel;

namespace Stellar.UnitTests.ViewModel;

/// <summary>
/// SelectionViewModel is the small selectable-item view model shipped for list scenarios.
/// Its ToggleSelected command is created during Bind, so it only exists once the view model
/// has been set up.
/// </summary>
public class SelectionViewModelTests
{
    [Fact]
    public void BeforeSetup_TheToggleCommandDoesNotExist()
    {
        var viewModel = new SelectionViewModel<int>();

        Assert.Null(viewModel.ToggleSelected);
    }

    [Fact]
    public void SetupViewModel_CreatesTheToggleCommand()
    {
        var viewModel = new SelectionViewModel<int>();

        viewModel.SetupViewModel();

        Assert.NotNull(viewModel.ToggleSelected);
    }

    [Fact]
    public void ToggleSelected_FlipsTheSelectedFlag()
    {
        var viewModel = new SelectionViewModel<int>();
        viewModel.SetupViewModel();

        Assert.False(viewModel.Selected);

        viewModel.ToggleSelected!.Execute().Subscribe();
        Assert.True(viewModel.Selected);

        viewModel.ToggleSelected!.Execute().Subscribe();
        Assert.False(viewModel.Selected);
    }

    [Fact]
    public void ToggleSelected_ReturnsTheNewState()
    {
        var viewModel = new SelectionViewModel<int>();
        viewModel.SetupViewModel();

        bool? returned = null;
        viewModel.ToggleSelected!.Execute().Subscribe(x => returned = x);

        Assert.True(returned);
    }

    [Fact]
    public void KeyAndDisplayValue_RoundTrip()
    {
        var viewModel = new SelectionViewModel<string>
        {
            Key = "abc",
            DisplayValue = "Display",
        };

        Assert.Equal("abc", viewModel.Key);
        Assert.Equal("Display", viewModel.DisplayValue);
    }

    [Fact]
    public void KeyAndDisplayValue_RaisePropertyChanged()
    {
        var viewModel = new SelectionViewModel<string>();
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        viewModel.Key = "abc";
        viewModel.DisplayValue = "Display";
        viewModel.Selected = true;

        Assert.Contains(nameof(SelectionViewModel<string>.Key), changed);
        Assert.Contains(nameof(SelectionViewModel<string>.DisplayValue), changed);
        Assert.Contains(nameof(SelectionViewModel<string>.Selected), changed);
    }

    [Fact]
    public void Unregister_ClearsBindingsAndReRegisterRebuildsTheCommand()
    {
        var viewModel = new SelectionViewModel<int>();
        viewModel.SetupViewModel();
        var original = viewModel.ToggleSelected!;

        viewModel.Unregister();

        Assert.False(viewModel.BindingsRegistered);

        viewModel.Register();

        Assert.True(viewModel.BindingsRegistered);
        Assert.NotSame(original, viewModel.ToggleSelected);
    }

    [Fact]
    public void AfterUnregister_TheCommandPropertyStillPointsAtTheOldCommand()
    {
        // Documents actual behaviour rather than the intuitive reading. Bind hands the
        // command to the binding disposables, so Unregister disposes it, but ReactiveUI
        // does not block execution on a disposed command and the property is not cleared.
        // Anything holding the old reference keeps working against a dead command.
        var viewModel = new SelectionViewModel<int>();
        viewModel.SetupViewModel();
        var command = viewModel.ToggleSelected!;

        viewModel.Unregister();

        Assert.NotNull(viewModel.ToggleSelected);
        Assert.Same(command, viewModel.ToggleSelected);
    }
}
