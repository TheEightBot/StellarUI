using System.Reactive.Subjects;

namespace Stellar;

public abstract class ViewManager<TViewModel> : IDisposable
    where TViewModel : class
{
    // Empty is handed out from every stream once disposed. Rx builds a new instance per
    // call, so these are cached rather than re-allocated on each property read.
    private static readonly IObservable<Unit> EmptyUnit = Observable.Empty<Unit>();
    private static readonly IObservable<LifecycleEvent> EmptyLifecycle = Observable.Empty<LifecycleEvent>();
    private static readonly IObservable<NavigationEvent> EmptyNavigation = Observable.Empty<NavigationEvent>();

    private readonly Lock _bindingLock = new();

    private readonly WeakCompositeDisposable _controlBindings;

    // A ViewManager exists per view, and most views subscribe to at most one or two of
    // these streams. Everything below is therefore created on first access rather than in
    // the constructor: the previous Lazy-per-stream approach allocated fourteen Lazy
    // instances and fourteen capturing closures for every view, used or not.
    private Subject<LifecycleEvent>? _lifecycleEvents;
    private Subject<NavigationEvent>? _navigationEvents;

    private IObservable<Unit>? _initialized;
    private IObservable<Unit>? _activated;
    private IObservable<Unit>? _attached;
    private IObservable<Unit>? _isAppearing;
    private IObservable<Unit>? _isDisappearing;
    private IObservable<Unit>? _detached;
    private IObservable<Unit>? _deactivated;
    private IObservable<Unit>? _disposed;
    private IObservable<LifecycleEvent>? _allLifecycleEvents;
    private IObservable<Unit>? _navigatedTo;
    private IObservable<Unit>? _navigatedFrom;
    private IObservable<NavigationEvent>? _allNavigationEvents;

    private bool _controlsBound;

    private bool _isDisposed = false;

    public IObservable<Unit> Initialized => LifecycleStream(ref _initialized, LifecycleEvent.Initialized);

    public IObservable<Unit> Activated => LifecycleStream(ref _activated, LifecycleEvent.Activated);

    public IObservable<Unit> Attached => LifecycleStream(ref _attached, LifecycleEvent.Attached);

    public IObservable<Unit> IsAppearing => LifecycleStream(ref _isAppearing, LifecycleEvent.IsAppearing);

    public IObservable<Unit> IsDisappearing => LifecycleStream(ref _isDisappearing, LifecycleEvent.IsDisappearing);

    public IObservable<Unit> Detached => LifecycleStream(ref _detached, LifecycleEvent.Detached);

    public IObservable<Unit> Deactivated => LifecycleStream(ref _deactivated, LifecycleEvent.Deactivated);

    public IObservable<Unit> Disposed => LifecycleStream(ref _disposed, LifecycleEvent.Disposed);

    public IObservable<LifecycleEvent> LifecycleEvents
    {
        get
        {
            if (Volatile.Read(ref _isDisposed))
            {
                return EmptyLifecycle;
            }

            // Checked before calling GetOrCreate so AsObservable is not evaluated, and so
            // allocated, on every read once the stream exists.
            var existing = Volatile.Read(ref _allLifecycleEvents);

            return existing ?? GetOrCreate(ref _allLifecycleEvents, LifecycleSubject.AsObservable());
        }
    }

    public IObservable<Unit> NavigatedTo => NavigationStream(ref _navigatedTo, NavigationEvent.NavigatedTo);

    public IObservable<Unit> NavigatedFrom => NavigationStream(ref _navigatedFrom, NavigationEvent.NavigatedFrom);

    public IObservable<NavigationEvent> NavigationEvents
    {
        get
        {
            if (Volatile.Read(ref _isDisposed))
            {
                return EmptyNavigation;
            }

            var existing = Volatile.Read(ref _allNavigationEvents);

            return existing ?? GetOrCreate(ref _allNavigationEvents, NavigationSubject.AsObservable());
        }
    }

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
    }

    /// <summary>
    /// Gets the lifecycle subject, creating it on first use.
    /// </summary>
    private Subject<LifecycleEvent> LifecycleSubject
    {
        get
        {
            var existing = Volatile.Read(ref _lifecycleEvents);
            if (existing is not null)
            {
                return existing;
            }

            var created = new Subject<LifecycleEvent>();

            // Losing the race is fine: the loser's subject was never published, so nobody
            // can be subscribed to it. Dispose it so it does not linger.
            var winner = Interlocked.CompareExchange(ref _lifecycleEvents, created, null);
            if (winner is null)
            {
                return created;
            }

            created.Dispose();
            return winner;
        }
    }

    /// <summary>
    /// Gets the navigation subject, creating it on first use.
    /// </summary>
    private Subject<NavigationEvent> NavigationSubject
    {
        get
        {
            var existing = Volatile.Read(ref _navigationEvents);
            if (existing is not null)
            {
                return existing;
            }

            var created = new Subject<NavigationEvent>();

            var winner = Interlocked.CompareExchange(ref _navigationEvents, created, null);
            if (winner is null)
            {
                return created;
            }

            created.Dispose();
            return winner;
        }
    }

    private static T GetOrCreate<T>(ref T? field, T created)
        where T : class
    {
        var existing = Volatile.Read(ref field);

        return existing ?? Interlocked.CompareExchange(ref field, created, null) ?? created;
    }

    private IObservable<Unit> LifecycleStream(ref IObservable<Unit>? field, LifecycleEvent lifecycleEvent)
    {
        if (Volatile.Read(ref _isDisposed))
        {
            return EmptyUnit;
        }

        var existing = Volatile.Read(ref field);

        return existing
            ?? GetOrCreate(
                ref field,
                LifecycleSubject.Where(x => x == lifecycleEvent).SelectUnit().AsObservable());
    }

    private IObservable<Unit> NavigationStream(ref IObservable<Unit>? field, NavigationEvent navigationEvent)
    {
        if (Volatile.Read(ref _isDisposed))
        {
            return EmptyUnit;
        }

        var existing = Volatile.Read(ref field);

        return existing
            ?? GetOrCreate(
                ref field,
                NavigationSubject.Where(x => x == navigationEvent).SelectUnit().AsObservable());
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

                // Null unless something actually subscribed, which is the common case.
                Volatile.Read(ref _lifecycleEvents)?.Dispose();
                Volatile.Read(ref _navigationEvents)?.Dispose();
            }
        }

        Volatile.Write(ref _isDisposed, true);
    }

    public void RegisterBindings(IStellarView<TViewModel> view)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _isDisposed), this);

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
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _isDisposed), this);

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
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _isDisposed), this);

        view.RegisterViewModelBindings();

        RegisterBindings(view);

        OnLifecycle(view, LifecycleEvent.Activated);
    }

    public virtual void HandleDeactivated(IStellarView<TViewModel> view)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _isDisposed), this);

        OnLifecycle(view, LifecycleEvent.Deactivated);

        UnregisterBindings(view);
    }

    public virtual void PropertyChanged<TView>(TView view, string? propertyName = null)
        where TView : IViewFor<TViewModel>
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _isDisposed), this);

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

        // Nothing has subscribed, so there is no subject to push through and no reason
        // to create one.
        Volatile.Read(ref _lifecycleEvents)?.OnNext(lifecycleEvent);
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

        Volatile.Read(ref _navigationEvents)?.OnNext(navigationEvent);
    }
}
