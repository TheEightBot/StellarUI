using System.Reactive.Subjects;
using ReactiveUI;

namespace Stellar;

public abstract class ViewManager : IDisposable
{
    private readonly Lazy<Subject<LifecycleEvent>> _lifecycle = new Lazy<Subject<LifecycleEvent>>(() => new Subject<LifecycleEvent>(), LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly object _bindingLock = new();

    private readonly CompositeDisposable _controlBindings = new();

    private bool _controlsBound;

    private bool _isDisposed;

    public IObservable<Unit> Activated => _lifecycle.Value.Where(x => x == LifecycleEvent.Activated).SelectUnit().AsObservable();

    public IObservable<Unit> Deactivated => _lifecycle.Value.Where(x => x == LifecycleEvent.Deactivated).SelectUnit().AsObservable();

    public IObservable<Unit> IsAppearing => _lifecycle.Value.Where(x => x == LifecycleEvent.IsAppearing).SelectUnit().AsObservable();

    public IObservable<Unit> IsDisappearing => _lifecycle.Value.Where(x => x == LifecycleEvent.IsDisappearing).SelectUnit().AsObservable();

    public IObservable<LifecycleEvent> Lifecycle => _lifecycle.Value.AsObservable();

    public bool Maintain { get; set; }

    public bool ControlsBound => Volatile.Read(ref _controlsBound);

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

    public void HandleActivated<TViewModel>(IStellarView<TViewModel> view)
        where TViewModel : class
    {
        view.RegisterViewModelBindings();

        RegisterBindings(view);

        OnLifecycle(LifecycleEvent.Activated);
    }

    public void HandleDeactivated<TViewModel>(IStellarView<TViewModel> view)
        where TViewModel : class
    {
        OnLifecycle(LifecycleEvent.Deactivated);

        UnregisterBindings(view);

        view.DisposeView();
    }

    public void PropertyChanged<TView, TViewModel>(TView view, string propertyName = null)
        where TView : IViewFor<TViewModel>
        where TViewModel : class
    {
        if (propertyName == nameof(IViewFor<TViewModel>.ViewModel) && view.ViewModel is not null)
        {
            view.SetupViewModel(view.ViewModel);
        }
    }

    public void OnLifecycle(LifecycleEvent lifecycleEvent)
    {
        if (!_lifecycle.IsValueCreated)
        {
            return;
        }

        _lifecycle.Value.OnNext(lifecycleEvent);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        if (disposing)
        {
            if (_lifecycle.IsValueCreated)
            {
                _lifecycle?.Value?.Dispose();
            }

            _controlBindings.Dispose();
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
