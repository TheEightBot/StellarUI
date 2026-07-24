using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using Stellar.ViewModel;

namespace Stellar.UnitTests;

public class ViewManagerTests
{
    [Fact]
    public void RegisterBindings_BindsTheViewAndMarksControlsBound()
    {
        using var manager = new TestViewManager();
        var view = new FakeView(manager);

        manager.RegisterBindings(view);

        Assert.True(manager.ControlsBound);
        Assert.Equal(1, view.BindCallCount);
    }

    [Fact]
    public void RegisterBindings_IsIdempotent()
    {
        using var manager = new TestViewManager();
        var view = new FakeView(manager);

        manager.RegisterBindings(view);
        manager.RegisterBindings(view);
        manager.RegisterBindings(view);

        // The second and third calls return early rather than rebinding.
        Assert.Equal(1, view.BindCallCount);
    }

    [Fact]
    public void RegisterBindings_EmitsInitialized()
    {
        using var manager = new TestViewManager();
        var view = new FakeView(manager);
        var events = new List<LifecycleEvent>();
        using var subscription = manager.LifecycleEvents.Subscribe(events.Add);

        manager.RegisterBindings(view);

        Assert.Equal(new[] { LifecycleEvent.Initialized }, events);
    }

    [Fact]
    public void UnregisterBindings_ClearsControlsBound()
    {
        using var manager = new TestViewManager();
        var view = new FakeView(manager);
        manager.RegisterBindings(view);

        manager.UnregisterBindings(view);

        Assert.False(manager.ControlsBound);
    }

    [Fact]
    public void UnregisterBindings_WhenMaintainIsSet_KeepsTheBindings()
    {
        // Maintain exists so singleton and scoped views survive deactivation with
        // their bindings intact. Losing this makes long-lived views silently stop
        // updating after the first navigation away.
        using var manager = new TestViewManager { Maintain = true };
        var view = new FakeView(manager);
        manager.RegisterBindings(view);

        manager.UnregisterBindings(view);

        Assert.True(manager.ControlsBound);
    }

    [Fact]
    public void HandleDeactivated_EmitsDeactivatedAndUnbinds()
    {
        using var manager = new TestViewManager();
        var view = new FakeView(manager);
        manager.RegisterBindings(view);

        var events = new List<LifecycleEvent>();
        using var subscription = manager.LifecycleEvents.Subscribe(events.Add);

        manager.HandleDeactivated(view);

        Assert.Contains(LifecycleEvent.Deactivated, events);
        Assert.False(manager.ControlsBound);
    }

    [Fact]
    public void HandleActivated_EmitsActivated()
    {
        using var manager = new TestViewManager();
        var view = new FakeView(manager);
        var events = new List<LifecycleEvent>();
        using var subscription = manager.LifecycleEvents.Subscribe(events.Add);

        manager.HandleActivated(view);

        Assert.Contains(LifecycleEvent.Activated, events);
    }

    [Fact]
    public void LifecycleStreams_FilterToTheirOwnEvent()
    {
        using var manager = new TestViewManager();
        var view = new FakeView(manager);

        var activated = 0;
        var detached = 0;
        using var a = manager.Activated.Subscribe(_ => activated++);
        using var d = manager.Detached.Subscribe(_ => detached++);

        manager.OnLifecycle(view, LifecycleEvent.Activated);
        manager.OnLifecycle(view, LifecycleEvent.Activated);
        manager.OnLifecycle(view, LifecycleEvent.Detached);

        Assert.Equal(2, activated);
        Assert.Equal(1, detached);
    }

    [Fact]
    public void NavigationStreams_FilterToTheirOwnEvent()
    {
        using var manager = new TestViewManager();
        var view = new FakeView(manager);

        var to = 0;
        var from = 0;
        using var a = manager.NavigatedTo.Subscribe(_ => to++);
        using var b = manager.NavigatedFrom.Subscribe(_ => from++);

        manager.OnNavigating(view, NavigationEvent.NavigatedTo);
        manager.OnNavigating(view, NavigationEvent.NavigatedFrom);
        manager.OnNavigating(view, NavigationEvent.NavigatedFrom);

        Assert.Equal(1, to);
        Assert.Equal(2, from);
    }

    [Fact]
    public void OnLifecycle_NotifiesAViewModelThatOptsIn()
    {
        using var manager = new TestViewManager();
        var viewModel = new LifecycleAwareViewModel();
        var view = new FakeView(manager) { ViewModel = viewModel };

        manager.OnLifecycle(view, LifecycleEvent.IsAppearing);

        Assert.Equal(new[] { LifecycleEvent.IsAppearing }, viewModel.Received);
    }

    [Fact]
    public void OnNavigating_NotifiesAViewModelThatOptsIn()
    {
        using var manager = new TestViewManager();
        var viewModel = new NavigationAwareViewModel();
        var view = new FakeView(manager) { ViewModel = viewModel };

        manager.OnNavigating(view, NavigationEvent.NavigatedTo);

        Assert.Equal(new[] { NavigationEvent.NavigatedTo }, viewModel.Received);
    }

    [Fact]
    public void AfterDispose_LifecycleStreamsAreEmpty()
    {
        var manager = new TestViewManager();
        manager.Dispose();

        var received = 0;
        using var subscription = manager.LifecycleEvents.Subscribe(_ => received++);

        Assert.Equal(0, received);
    }

    [Fact]
    public void AfterDispose_RegisterBindingsThrows()
    {
        var manager = new TestViewManager();
        var view = new FakeView(manager);
        manager.Dispose();

        Assert.Throws<ObjectDisposedException>(() => manager.RegisterBindings(view));
    }

    [Fact]
    public void AfterDispose_OnLifecycleIsSilentRatherThanThrowing()
    {
        // Deliberately different from RegisterBindings: lifecycle callbacks can
        // arrive from the platform during teardown, so they must not throw.
        var manager = new TestViewManager();
        var view = new FakeView(manager);
        manager.Dispose();

        manager.OnLifecycle(view, LifecycleEvent.Disposed);
        manager.OnNavigating(view, NavigationEvent.NavigatedFrom);
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var manager = new TestViewManager();

        manager.Dispose();
        manager.Dispose();
    }

    private sealed class TestViewManager : ViewManager<object>
    {
    }

    private sealed class FakeView : IStellarView<object>
    {
        public FakeView(ViewManager<object> viewManager) => ViewManager = viewManager;

        public ViewManager<object> ViewManager { get; }

        public object? ViewModel { get; set; }

        object? IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = value;
        }

        public int BindCallCount { get; private set; }

        public void Bind(WeakCompositeDisposable disposables) => BindCallCount++;

        public void Initialize()
        {
        }

        public void SetupUserInterface()
        {
        }
    }

    private sealed class LifecycleAwareViewModel : ILifecycleEventAware
    {
        public List<LifecycleEvent> Received { get; } = new();

        public void OnLifecycleEvent(LifecycleEvent lifecycleEvent) => Received.Add(lifecycleEvent);
    }

    private sealed class NavigationAwareViewModel : INavigationEventAware
    {
        public List<NavigationEvent> Received { get; } = new();

        public void OnNavigationEvent(NavigationEvent navigationEvent) => Received.Add(navigationEvent);
    }
}
