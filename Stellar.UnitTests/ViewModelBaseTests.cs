using System.Reactive.Disposables;
using Stellar.ViewModel;

namespace Stellar.UnitTests;

public class ViewModelBaseTests
{
    [Fact]
    public void SetupViewModel_InitializesOnce()
    {
        // Arrange
        var viewModel = new TestViewModel();

        // Act
        viewModel.SetupViewModel();

        // Assert
        Assert.True(viewModel.Initialized);
    }

    [Fact]
    public void RegisterBindings_RegistersOnce()
    {
        // Arrange
        var viewModel = new TestViewModel();

        // Act
        viewModel.Register();

        // Assert
        Assert.True(viewModel.BindingsRegistered);
    }

    [Fact]
    public void UnregisterBindings_ClearsBindings()
    {
        // Arrange
        var viewModel = new TestViewModel();
        viewModel.Register();

        // Act
        viewModel.Unregister();

        // Assert
        Assert.False(viewModel.BindingsRegistered);
    }

    private class TestViewModel : ViewModelBase
    {
        protected override void Bind(CompositeDisposable disposables)
        {
            // Do nothing
        }
    }
}
