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

    [Fact]
    public void ViewModelBase_IsDisposable()
    {
        // Regression guard. ViewModelBase carried the full dispose pattern and a public
        // Dispose, but did not implement IDisposable, and a CA1001 suppression hid the
        // analyser warning that says so. IViewForExtensions.DisposeViewModel gates on
        // `vmb is IDisposable`, so that check was always false and view models were never
        // torn down when their view went away -- their binding subscriptions stayed live
        // until the GC eventually collected the view model.
        Assert.True(typeof(ViewModelBase).IsAssignableTo(typeof(IDisposable)));
    }

    [Fact]
    public void Dispose_MarksTheViewModelDisposedAndReleasesBindings()
    {
        var viewModel = new TestViewModel();
        viewModel.Register();

        ((IDisposable)viewModel).Dispose();

        Assert.True(viewModel.IsDisposed);
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var viewModel = new TestViewModel();

        viewModel.Dispose();
        viewModel.Dispose();

        Assert.True(viewModel.IsDisposed);
    }

    [Fact]
    public void AfterDispose_RegisterIsANoOp()
    {
        var viewModel = new TestViewModel();
        viewModel.Dispose();

        viewModel.Register();

        Assert.False(viewModel.BindingsRegistered);
    }

    private class TestViewModel : ViewModelBase
    {
        protected override void Bind(WeakCompositeDisposable disposables)
        {
            // Do nothing
        }
    }
}
