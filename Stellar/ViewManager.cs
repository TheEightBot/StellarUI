using System.Reactive.Subjects;
using ReactiveUI;

namespace Stellar;

#pragma warning disable CA1001
public abstract class ViewManager
#pragma warning restore CA1001
{
    private readonly Lazy<Subject<LifecycleEvent>> _lifecycleEvents = new Lazy<Subject<LifecycleEvent>>(() => new Subject<LifecycleEvent>(), LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly Lazy<Subject<NavigationEvent>> _navigationEvents = new Lazy<Subject<NavigationEvent>>(() => new Subject<NavigationEvent>(), LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly object _bindingLock = new();

    private readonly CompositeDisposable _controlBindings = new();

    private bool _controlsBound;

    public IObservable<Unit> Initialized => _lifecycleEvents.Value.Where(x => x == LifecycleEvent.Initialized).SelectUnit().AsObservable();

    public IObservable<Unit> Activated => _lifecycleEvents.Value.Where(x => x == LifecycleEvent.Activated).SelectUnit().AsObservable();

    public IObservable<Unit> Attached => _lifecycleEvents.Value.Where(x => x == LifecycleEvent.Attached).SelectUnit().AsObservable();

    public IObservable<Unit> IsAppearing => _lifecycleEvents.Value.Where(x => x == LifecycleEvent.IsAppearing).SelectUnit().AsObservable();

    public IObservable<Unit> IsDisappearing => _lifecycleEvents.Value.Where(x => x == LifecycleEvent.IsDisappearing).SelectUnit().AsObservable();

    public IObservable<Unit> Detached => _lifecycleEvents.Value.Where(x => x == LifecycleEvent.Detached).SelectUnit().AsObservable();

    public IObservable<Unit> Deactivated => _lifecycleEvents.Value.Where(x => x == LifecycleEvent.Deactivated).SelectUnit().AsObservable();

    public IObservable<Unit> Disposed => _lifecycleEvents.Value.Where(x => x == LifecycleEvent.Disposed).SelectUnit().AsObservable();

    public IObservable<LifecycleEvent> LifecycleEvents => _lifecycleEvents.Value.AsObservable();

    public IObservable<Unit> NavigatedTo => _navigationEvents.Value.Where(x => x == NavigationEvent.NavigatedTo).SelectUnit().AsObservable();

    public IObservable<Unit> NavigatedFrom => _navigationEvents.Value.Where(x => x == NavigationEvent.NavigatedFrom).SelectUnit().AsObservable();

    public IObservable<NavigationEvent> NavigationEvents => _navigationEvents.Value.AsObservable();

    public bool Maintain { get; set; }

    public bool ControlsBound => Volatile.Read(ref _controlsBound);

    ~ViewManager()
    {
        if (_lifecycleEvents.IsValueCreated)
        {
            _lifecycleEvents?.Value?.Dispose();
        }

        _controlBindings.Dispose();
    }

    public void RegisterBindings<TViewModel>(IStellarView<TViewModel> view)
        where TViewModel : class
    {
        lock (_bindingLock)
        {
            if (_controlsBound)
            {
                return;
            }

            view.RegisterViewModelBindings();

            _controlBindings.Clear();
            view.Bind(_controlBindings);

            Volatile.Write(ref _controlsBound, true);

            OnLifecycle(view, LifecycleEvent.Initialized);
        }
    }

    public void UnregisterBindings<TViewModel>(IStellarView<TViewModel> view)
        where TViewModel : class
    {
        lock (_bindingLock)
        {
            if (Maintain || !_controlsBound)
            {
                return;
            }

            _controlBindings.Clear();

            view.UnregisterViewModelBindings();

            Volatile.Write(ref _controlsBound, false);
        }
    }

    public virtual void HandleActivated<TViewModel>(IStellarView<TViewModel> view)
        where TViewModel : class
    {
        view.RegisterViewModelBindings();

        RegisterBindings(view);

        OnLifecycle(view, LifecycleEvent.Activated);
    }

    public virtual void HandleDeactivated<TViewModel>(IStellarView<TViewModel> view)
        where TViewModel : class
    {
        OnLifecycle(view, LifecycleEvent.Deactivated);

        UnregisterBindings(view);
    }

    public void PropertyChanged<TView, TViewModel>(TView view, string? propertyName = null)
        where TView : IViewFor<TViewModel>
        where TViewModel : class
    {
        if (propertyName == nameof(IViewFor<TViewModel>.ViewModel) && view.ViewModel is not null)
        {
            view.SetupViewModel(view.ViewModel);
        }
    }

    public void OnLifecycle<TViewModel>(IStellarView<TViewModel> view, LifecycleEvent lifecycleEvent)
        where TViewModel : class
    {
        if (view.ViewModel is ILifecycleEventAware lea)
        {
            lea.OnLifecycleEvent(lifecycleEvent);
        }

        if (!_lifecycleEvents.IsValueCreated)
        {
            return;
        }

        _lifecycleEvents.Value.OnNext(lifecycleEvent);
    }

    public void OnNavigating<TViewModel>(IStellarView<TViewModel> view, NavigationEvent navigationEvent)
        where TViewModel : class
    {
        if (view.ViewModel is INavigationEventAware nea)
        {
            nea.OnNavigationEvent(navigationEvent);
        }

        if (!_navigationEvents.IsValueCreated)
        {
            return;
        }

        _navigationEvents.Value.OnNext(navigationEvent);
    }
}
