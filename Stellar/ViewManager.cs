using System.Reactive.Subjects;

namespace Stellar;

public abstract class ViewManager<TViewModel> : IDisposable
    where TViewModel : class
{
    private readonly Lazy<Subject<LifecycleEvent>> _lifecycleEvents;

    private readonly Lazy<Subject<NavigationEvent>> _navigationEvents;

    private readonly Lock _bindingLock = new();

    private readonly WeakCompositeDisposable _controlBindings;

    private readonly Lazy<IObservable<Unit>> _initialized;
    private readonly Lazy<IObservable<Unit>> _activated;
    private readonly Lazy<IObservable<Unit>> _attached;
    private readonly Lazy<IObservable<Unit>> _isAppearing;
    private readonly Lazy<IObservable<Unit>> _isDisappearing;
    private readonly Lazy<IObservable<Unit>> _detached;
    private readonly Lazy<IObservable<Unit>> _deactivated;
    private readonly Lazy<IObservable<Unit>> _disposed;
    private readonly Lazy<IObservable<LifecycleEvent>> _allLifecycleEvents;
    private readonly Lazy<IObservable<Unit>> _navigatedTo;
    private readonly Lazy<IObservable<Unit>> _navigatedFrom;
    private readonly Lazy<IObservable<NavigationEvent>> _allNavigationEvents;

    private bool _controlsBound;

    private bool _isDisposed = false;

    public IObservable<Unit> Initialized => Volatile.Read(ref _isDisposed) ? Observable.Empty<Unit>() : _initialized.Value;

    public IObservable<Unit> Activated => Volatile.Read(ref _isDisposed) ? Observable.Empty<Unit>() : _activated.Value;

    public IObservable<Unit> Attached => Volatile.Read(ref _isDisposed) ? Observable.Empty<Unit>() : _attached.Value;

    public IObservable<Unit> IsAppearing => Volatile.Read(ref _isDisposed) ? Observable.Empty<Unit>() : _isAppearing.Value;

    public IObservable<Unit> IsDisappearing => Volatile.Read(ref _isDisposed) ? Observable.Empty<Unit>() : _isDisappearing.Value;

    public IObservable<Unit> Detached => Volatile.Read(ref _isDisposed) ? Observable.Empty<Unit>() : _detached.Value;

    public IObservable<Unit> Deactivated => Volatile.Read(ref _isDisposed) ? Observable.Empty<Unit>() : _deactivated.Value;

    public IObservable<Unit> Disposed => Volatile.Read(ref _isDisposed) ? Observable.Empty<Unit>() : _disposed.Value;

    public IObservable<LifecycleEvent> LifecycleEvents => Volatile.Read(ref _isDisposed) ? Observable.Empty<LifecycleEvent>() : _allLifecycleEvents.Value;

    public IObservable<Unit> NavigatedTo => Volatile.Read(ref _isDisposed) ? Observable.Empty<Unit>() : _navigatedTo.Value;

    public IObservable<Unit> NavigatedFrom => Volatile.Read(ref _isDisposed) ? Observable.Empty<Unit>() : _navigatedFrom.Value;

    public IObservable<NavigationEvent> NavigationEvents => Volatile.Read(ref _isDisposed) ? Observable.Empty<NavigationEvent>() : _allNavigationEvents.Value;

    public bool Maintain { get; set; }

    public bool ControlsBound
    {
        get
        {
            lock (_bindingLock)
            {
                return _controlsBound;
            }
        }
    }

    public ViewManager()
    {
        _controlBindings = new(this);

        _lifecycleEvents = new(() => new Subject<LifecycleEvent>(), LazyThreadSafetyMode.ExecutionAndPublication);
        _navigationEvents = new(() => new Subject<NavigationEvent>(), LazyThreadSafetyMode.ExecutionAndPublication);

        _initialized = new(() => _lifecycleEvents.Value.Where(x => x == LifecycleEvent.Initialized).SelectUnit().AsObservable());
        _activated = new(() => _lifecycleEvents.Value.Where(x => x == LifecycleEvent.Activated).SelectUnit().AsObservable());
        _attached = new(() => _lifecycleEvents.Value.Where(x => x == LifecycleEvent.Attached).SelectUnit().AsObservable());
        _isAppearing = new(() => _lifecycleEvents.Value.Where(x => x == LifecycleEvent.IsAppearing).SelectUnit().AsObservable());
        _isDisappearing = new(() => _lifecycleEvents.Value.Where(x => x == LifecycleEvent.IsDisappearing).SelectUnit().AsObservable());
        _detached = new(() => _lifecycleEvents.Value.Where(x => x == LifecycleEvent.Detached).SelectUnit().AsObservable());
        _deactivated = new(() => _lifecycleEvents.Value.Where(x => x == LifecycleEvent.Deactivated).SelectUnit().AsObservable());
        _disposed = new(() => _lifecycleEvents.Value.Where(x => x == LifecycleEvent.Disposed).SelectUnit().AsObservable());
        _allLifecycleEvents = new(() => _lifecycleEvents.Value.AsObservable());
        _navigatedTo = new(() => _navigationEvents.Value.Where(x => x == NavigationEvent.NavigatedTo).SelectUnit().AsObservable());
        _navigatedFrom = new(() => _navigationEvents.Value.Where(x => x == NavigationEvent.NavigatedFrom).SelectUnit().AsObservable());
        _allNavigationEvents = new(() => _navigationEvents.Value.AsObservable());
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (Volatile.Read(ref _isDisposed))
        {
            return;
        }

        if (disposing)
        {
            lock (_bindingLock)
            {
                _controlBindings.Dispose();

                // Dispose lazy-created subjects if they were created
                if (_lifecycleEvents.IsValueCreated)
                {
                    _lifecycleEvents.Value.Dispose();
                }

                if (_navigationEvents.IsValueCreated)
                {
                    _navigationEvents.Value.Dispose();
                }
            }
        }

        Volatile.Write(ref _isDisposed, true);
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _isDisposed))
        {
            throw new ObjectDisposedException(nameof(ViewManager<TViewModel>));
        }
    }

    public void RegisterBindings(IStellarView<TViewModel> view)
    {
        ThrowIfDisposed();

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

    public void UnregisterBindings(IStellarView<TViewModel> view)
    {
        ThrowIfDisposed();

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

    public virtual void HandleActivated(IStellarView<TViewModel> view)
    {
        ThrowIfDisposed();

        view.RegisterViewModelBindings();

        RegisterBindings(view);

        OnLifecycle(view, LifecycleEvent.Activated);
    }

    public virtual void HandleDeactivated(IStellarView<TViewModel> view)
    {
        ThrowIfDisposed();

        OnLifecycle(view, LifecycleEvent.Deactivated);

        UnregisterBindings(view);
    }

    public virtual void PropertyChanged<TView>(TView view, string? propertyName = null)
        where TView : IViewFor<TViewModel>
    {
        ThrowIfDisposed();

        if (propertyName == nameof(IViewFor<TViewModel>.ViewModel) && view.ViewModel is not null)
        {
            view.SetupViewModel(view.ViewModel);
        }
    }

    public void OnLifecycle(IStellarView<TViewModel> view, LifecycleEvent lifecycleEvent)
    {
        if (Volatile.Read(ref _isDisposed))
        {
            return; // Silently return if disposed to avoid exceptions during cleanup
        }

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

    public void OnNavigating(IStellarView<TViewModel> view, NavigationEvent navigationEvent)
    {
        if (Volatile.Read(ref _isDisposed))
        {
            return; // Silently return if disposed to avoid exceptions during cleanup
        }

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
