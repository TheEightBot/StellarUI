using System.Reactive;
using ReactiveUI;
using Stellar.ViewModel;

namespace Stellar.UnitTests.Extensions;

/// <summary>
/// The extension methods that drive a view through its lifecycle: wiring a view model,
/// running the initialisation sequence, and tearing both down again.
/// </summary>
public class StellarViewExtensionsTests
{
    [Fact]
    public void InitializeStellarComponent_RunsTheSetupSequenceInOrder()
    {
        var viewModel = new TestViewModel();
        var view = new FakeStellarView();

        view.InitializeStellarComponent(viewModel);

        Assert.Equal(
            new[] { nameof(FakeStellarView.Initialize), nameof(FakeStellarView.SetupUserInterface), nameof(FakeStellarView.Bind) },
            view.Calls);
    }

    [Fact]
    public void InitializeStellarComponent_AssignsAndSetsUpTheViewModel()
    {
        var viewModel = new TestViewModel();
        var view = new FakeStellarView();

        view.InitializeStellarComponent(viewModel);

        Assert.Same(viewModel, view.ViewModel);
        Assert.True(viewModel.Initialized);
        Assert.True(viewModel.BindingsRegistered);
    }

    [Fact]
    public void InitializeStellarComponent_RegistersBindingsByDefault()
    {
        var view = new FakeStellarView();

        view.InitializeStellarComponent(new TestViewModel());

        Assert.True(view.ViewManager.ControlsBound);
    }

    [Fact]
    public void DelayBindingRegistrationUntilAttached_LeavesTheViewUnbound()
    {
        var view = new FakeStellarView();

        view.InitializeStellarComponent(new TestViewModel(), delayBindingRegistrationUntilAttached: true);

        Assert.False(view.ViewManager.ControlsBound);
        Assert.DoesNotContain(nameof(FakeStellarView.Bind), view.Calls);
    }

    [Fact]
    public void Maintain_PropagatesToTheViewManager()
    {
        var view = new FakeStellarView();

        view.InitializeStellarComponent(new TestViewModel(), maintain: true);

        Assert.True(view.ViewManager.Maintain);
    }

    [Fact]
    public void AServiceRegisteredSingletonView_IsMaintainedAutomatically()
    {
        // A singleton or scoped view outlives a single navigation, so its bindings must
        // survive deactivation without the caller asking for it.
        var view = new SingletonStellarView();

        view.InitializeStellarComponent(new TestViewModel());

        Assert.True(view.ViewManager.Maintain);
    }

    [Fact]
    public void ATransientView_IsNotMaintained()
    {
        var view = new FakeStellarView();

        view.InitializeStellarComponent(new TestViewModel());

        Assert.False(view.ViewManager.Maintain);
    }

    [Fact]
    public void ManageDispose_EmitsDisposedAndClearsTheViewModel()
    {
        var view = new FakeStellarView();
        view.InitializeStellarComponent(new TestViewModel());

        var events = new List<LifecycleEvent>();
        using var subscription = view.ViewManager.LifecycleEvents.Subscribe(events.Add);

        var isDisposed = false;
        view.ManageDispose(disposing: true, ref isDisposed);

        Assert.True(isDisposed);
        Assert.Contains(LifecycleEvent.Disposed, events);
        Assert.Null(view.ViewModel);
    }

    [Fact]
    public void ManageDispose_IsIdempotent()
    {
        var view = new FakeStellarView();
        view.InitializeStellarComponent(new TestViewModel());

        var events = new List<LifecycleEvent>();
        using var subscription = view.ViewManager.LifecycleEvents.Subscribe(events.Add);

        var isDisposed = false;
        view.ManageDispose(disposing: true, ref isDisposed);
        view.ManageDispose(disposing: true, ref isDisposed);

        Assert.Single(events, static e => e == LifecycleEvent.Disposed);
    }

    [Fact]
    public void ManageDispose_WhenNotDisposing_MarksDisposedWithoutTearingDown()
    {
        var viewModel = new TestViewModel();
        var view = new FakeStellarView();
        view.InitializeStellarComponent(viewModel);

        var isDisposed = false;
        view.ManageDispose(disposing: false, ref isDisposed);

        Assert.True(isDisposed);
        Assert.Same(viewModel, view.ViewModel);
    }

    [Fact]
    public void RegisterViewModelBindings_MovesTheViewModelIntoTheRegisteredState()
    {
        var viewModel = new TestViewModel();
        var view = new FakeStellarView { ViewModel = viewModel };

        view.RegisterViewModelBindings();
        Assert.True(viewModel.BindingsRegistered);

        view.UnregisterViewModelBindings();
        Assert.False(viewModel.BindingsRegistered);
    }

    [Fact]
    public void SetupViewModel_ReplacesADifferentViewModel()
    {
        var first = new TestViewModel();
        var second = new TestViewModel();
        var view = new FakeStellarView { ViewModel = first };

        view.SetupViewModel(second);

        Assert.Same(second, view.ViewModel);
        Assert.True(second.Initialized);
    }

    [Fact]
    public void SetupViewModel_WithNull_LeavesTheExistingViewModelInPlace()
    {
        var existing = new TestViewModel();
        var view = new FakeStellarView { ViewModel = existing };

        view.SetupViewModel(null);

        Assert.Same(existing, view.ViewModel);
        Assert.True(existing.Initialized);
    }

    [Fact]
    public void DisposeViewModel_DisposesAndClearsIt()
    {
        var viewModel = new TestViewModel();
        var view = new FakeStellarView { ViewModel = viewModel };

        view.DisposeViewModel();

        Assert.True(viewModel.IsDisposed);
        Assert.Null(view.ViewModel);
    }

    [Fact]
    public void DisposeViewModel_HonoursMaintain()
    {
        // A maintained view model is shared, so tearing down one view must not dispose it.
        var viewModel = new TestViewModel { Maintain = true };
        var view = new FakeStellarView { ViewModel = viewModel };

        view.DisposeViewModel();

        Assert.False(viewModel.IsDisposed);
        Assert.Null(view.ViewModel);
    }

    [Fact]
    public void DisposeView_DisposesADisposableView()
    {
        var view = new DisposableStellarView();

        view.DisposeView();

        Assert.True(view.WasDisposed);
    }

    [Fact]
    public void DisposeView_HonoursMaintain()
    {
        var view = new DisposableStellarView();
        view.ViewManager.Maintain = true;

        view.DisposeView();

        Assert.False(view.WasDisposed);
    }

    private class TestViewModel : ViewModelBase
    {
        protected override void Bind(WeakCompositeDisposable disposables)
        {
        }
    }

    private class FakeStellarView : IStellarView<TestViewModel>
    {
        public List<string> Calls { get; } = new();

        public ViewManager<TestViewModel> ViewManager { get; } = new TestViewManager();

        public TestViewModel? ViewModel { get; set; }

        object? IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = value as TestViewModel;
        }

        public void Initialize() => Calls.Add(nameof(Initialize));

        public void SetupUserInterface() => Calls.Add(nameof(SetupUserInterface));

        public void Bind(WeakCompositeDisposable disposables) => Calls.Add(nameof(Bind));
    }

    [ServiceRegistration(Lifetime.Singleton)]
    private sealed class SingletonStellarView : FakeStellarView;

    private sealed class DisposableStellarView : FakeStellarView, IDisposable
    {
        public bool WasDisposed { get; private set; }

        public void Dispose() => WasDisposed = true;
    }

    private sealed class TestViewManager : ViewManager<TestViewModel>;
}
