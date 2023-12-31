using System.Reactive.Subjects;
using ReactiveUI;

namespace Stellar;

#pragma warning disable CA1001
public abstract class ViewManager
#pragma warning restore CA1001
{
    private readonly Lazy<Subject<LifecycleEvent>> _lifecycle = new Lazy<Subject<LifecycleEvent>>(() => new Subject<LifecycleEvent>(), LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly object _bindingLock = new();

    private readonly CompositeDisposable _controlBindings = new();

    private bool _controlsBound;

    public IObservable<Unit> Initialized => _lifecycle.Value.Where(x => x == LifecycleEvent.Initialized).SelectUnit().AsObservable();

    public IObservable<Unit> Activated => _lifecycle.Value.Where(x => x == LifecycleEvent.Activated).SelectUnit().AsObservable();

    public IObservable<Unit> Attached => _lifecycle.Value.Where(x => x == LifecycleEvent.Attached).SelectUnit().AsObservable();

    public IObservable<Unit> IsAppearing => _lifecycle.Value.Where(x => x == LifecycleEvent.IsAppearing).SelectUnit().AsObservable();

    public IObservable<Unit> IsDisappearing => _lifecycle.Value.Where(x => x == LifecycleEvent.IsDisappearing).SelectUnit().AsObservable();

    public IObservable<Unit> Detached => _lifecycle.Value.Where(x => x == LifecycleEvent.Detached).SelectUnit().AsObservable();

    public IObservable<Unit> Deactivated => _lifecycle.Value.Where(x => x == LifecycleEvent.Deactivated).SelectUnit().AsObservable();

    public IObservable<Unit> Disposed => _lifecycle.Value.Where(x => x == LifecycleEvent.Disposed).SelectUnit().AsObservable();

    public IObservable<LifecycleEvent> Lifecycle => _lifecycle.Value.AsObservable();

    public bool Maintain { get; set; }

    public bool ControlsBound => Volatile.Read(ref _controlsBound);

    ~ViewManager()
    {
        if (_lifecycle.IsValueCreated)
        {
            _lifecycle?.Value?.Dispose();
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

        if (!_lifecycle.IsValueCreated)
        {
            return;
        }

        _lifecycle.Value.OnNext(lifecycleEvent);
    }
}
