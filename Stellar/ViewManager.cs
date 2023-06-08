using System.Reactive.Subjects;
using ReactiveUI;

namespace Stellar;

public abstract class ViewManager : IDisposable
{
    private readonly Lazy<Subject<LifecycleEvent>> _lifecycle = new Lazy<Subject<LifecycleEvent>>(() => new Subject<LifecycleEvent>(), LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly object _bindingLock = new();

    private bool _controlsBound;

    private bool _isDisposed;

    public IObservable<Unit> Activated => _lifecycle.Value.Where(x => x == LifecycleEvent.Activated).SelectUnit().AsObservable();

    public IObservable<Unit> Deactivated => _lifecycle.Value.Where(x => x == LifecycleEvent.Deactivated).SelectUnit().AsObservable();

    public IObservable<Unit> IsAppearing => _lifecycle.Value.Where(x => x == LifecycleEvent.IsAppearing).SelectUnit().AsObservable();

    public IObservable<Unit> IsDisappearing => _lifecycle.Value.Where(x => x == LifecycleEvent.IsDisappearing).SelectUnit().AsObservable();

    public IObservable<LifecycleEvent> Lifecycle => _lifecycle.Value.AsObservable();

    public bool Maintain { get; set; }

    public CompositeDisposable ControlBindings { get; } = new();

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

            view.BindControls();

            Volatile.Write(ref _controlsBound, true);
        }
    }

    public void UnregisterBindings<TViewModel>(IStellarView<TViewModel> view)
        where TViewModel : class
    {
        lock (_bindingLock)
        {
            if (view.Maintain || !_controlsBound)
            {
                return;
            }

            ControlBindings?.Clear();

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

    public void PropertyChanged<TView, TViewModel>(TView visualElement, string propertyName = null)
        where TView : IViewFor<TViewModel>
        where TViewModel : class
    {
        if (propertyName == nameof(IViewFor<TViewModel>.ViewModel) && visualElement.ViewModel is not null)
        {
            visualElement.SetupViewModel(visualElement.ViewModel);
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

            this.ControlBindings?.Dispose();
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
