using System.Reactive.Subjects;

namespace Stellar;

public abstract class ViewManager<TViewModel> : IDisposable
    where TViewModel : class
{
    private readonly Lazy<Subject<LifecycleEvent>> _lifecycleEvents;

    private readonly Lazy<Subject<NavigationEvent>> _navigationEvents;

    private readonly Lazy<IObservable<Unit>> _initialized;

    private readonly Lazy<IObservable<Unit>> _activated;

    private readonly Lazy<IObservable<Unit>> _attached;

    private readonly Lazy<IObservable<Unit>> _isAppearing;

    private readonly Lazy<IObservable<Unit>> _isDisappearing;

    private readonly Lazy<IObservable<Unit>> _detached;

    private readonly Lazy<IObservable<Unit>> _deactivated;

    private readonly Lazy<IObservable<Unit>> _disposedObservable;

    private readonly Lazy<IObservable<Unit>> _navigatedTo;

    private readonly Lazy<IObservable<Unit>> _navigatedFrom;

    private readonly Lock _bindingLock = new();

    private readonly WeakCompositeDisposable _controlBindings;

    private bool _controlsBound;

    private bool _disposed = false;

    public IObservable<Unit> Initialized => _disposed || !_lifecycleEvents.IsValueCreated ? Observable.Empty<Unit>() : _initialized.Value;

    public IObservable<Unit> Activated => _disposed || !_lifecycleEvents.IsValueCreated ? Observable.Empty<Unit>() : _activated.Value;

    public IObservable<Unit> Attached => _disposed || !_lifecycleEvents.IsValueCreated ? Observable.Empty<Unit>() : _attached.Value;

    public IObservable<Unit> IsAppearing => _disposed || !_lifecycleEvents.IsValueCreated ? Observable.Empty<Unit>() : _isAppearing.Value;

    public IObservable<Unit> IsDisappearing => _disposed || !_lifecycleEvents.IsValueCreated ? Observable.Empty<Unit>() : _isDisappearing.Value;

    public IObservable<Unit> Detached => _disposed || !_lifecycleEvents.IsValueCreated ? Observable.Empty<Unit>() : _detached.Value;

    public IObservable<Unit> Deactivated => _disposed || !_lifecycleEvents.IsValueCreated ? Observable.Empty<Unit>() : _deactivated.Value;

    public IObservable<Unit> Disposed => _disposed || !_lifecycleEvents.IsValueCreated ? Observable.Empty<Unit>() : _disposedObservable.Value;

    public IObservable<LifecycleEvent> LifecycleEvents => _disposed || !_lifecycleEvents.IsValueCreated ? Observable.Empty<LifecycleEvent>() : _lifecycleEvents.Value.AsObservable();

    public IObservable<Unit> NavigatedTo => _disposed || !_navigationEvents.IsValueCreated ? Observable.Empty<Unit>() : _navigatedTo.Value;

    public IObservable<Unit> NavigatedFrom => _disposed || !_navigationEvents.IsValueCreated ? Observable.Empty<Unit>() : _navigatedFrom.Value;

    public IObservable<NavigationEvent> NavigationEvents => _disposed || !_navigationEvents.IsValueCreated ? Observable.Empty<NavigationEvent>() : _navigationEvents.Value.AsObservable();

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

        _initialized = new(() => _lifecycleEvents.Value.Where(static x => x == LifecycleEvent.Initialized).SelectUnit().AsObservable(), LazyThreadSafetyMode.ExecutionAndPublication);
        _activated = new(() => _lifecycleEvents.Value.Where(static x => x == LifecycleEvent.Activated).SelectUnit().AsObservable(), LazyThreadSafetyMode.ExecutionAndPublication);
        _attached = new(() => _lifecycleEvents.Value.Where(static x => x == LifecycleEvent.Attached).SelectUnit().AsObservable(), LazyThreadSafetyMode.ExecutionAndPublication);
        _isAppearing = new(() => _lifecycleEvents.Value.Where(static x => x == LifecycleEvent.IsAppearing).SelectUnit().AsObservable(), LazyThreadSafetyMode.ExecutionAndPublication);
        _isDisappearing = new(() => _lifecycleEvents.Value.Where(static x => x == LifecycleEvent.IsDisappearing).SelectUnit().AsObservable(), LazyThreadSafetyMode.ExecutionAndPublication);
        _detached = new(() => _lifecycleEvents.Value.Where(static x => x == LifecycleEvent.Detached).SelectUnit().AsObservable(), LazyThreadSafetyMode.ExecutionAndPublication);
        _deactivated = new(() => _lifecycleEvents.Value.Where(static x => x == LifecycleEvent.Deactivated).SelectUnit().AsObservable(), LazyThreadSafetyMode.ExecutionAndPublication);
        _disposedObservable = new(() => _lifecycleEvents.Value.Where(static x => x == LifecycleEvent.Disposed).SelectUnit().AsObservable(), LazyThreadSafetyMode.ExecutionAndPublication);

        _navigatedTo = new(() => _navigationEvents.Value.Where(static x => x == NavigationEvent.NavigatedTo).SelectUnit().AsObservable(), LazyThreadSafetyMode.ExecutionAndPublication);
        _navigatedFrom = new(() => _navigationEvents.Value.Where(static x => x == NavigationEvent.NavigatedFrom).SelectUnit().AsObservable(), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
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

        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
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
        if (_disposed)
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
        if (_disposed)
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
