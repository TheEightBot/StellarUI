namespace EightBot.Stellar.Maui;

public class ViewManager : IDisposable
{
    private readonly Lazy<Subject<LifecycleEvent>> _lifecycle = new Lazy<Subject<LifecycleEvent>>(() => new Subject<LifecycleEvent>(), LazyThreadSafetyMode.ExecutionAndPublication);

    public IObservable<Unit> Activated => _lifecycle.Value.Where(x => x == LifecycleEvent.Activated).SelectUnit().AsObservable();

    public IObservable<Unit> Deactivated => _lifecycle.Value.Where(x => x == LifecycleEvent.Deactivated).SelectUnit().AsObservable();

    public IObservable<Unit> IsAppearing => _lifecycle.Value.Where(x => x == LifecycleEvent.IsAppearing).SelectUnit().AsObservable();

    public IObservable<Unit> IsDisappearing => _lifecycle.Value.Where(x => x == LifecycleEvent.IsDisappearing).SelectUnit().AsObservable();

    public IObservable<LifecycleEvent> Lifecycle => _lifecycle.Value.AsObservable();

    private readonly object _bindingLock = new object();

    private bool _controlsBound;

    private bool _disposedValue;

    public CompositeDisposable ControlBindings { get; } = new CompositeDisposable();

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

            Volatile.Write(ref _controlsBound, true);

            view.RegisterViewModelBindings();

            view.BindControls();
        }
    }

    public void UnregisterBindings<TViewModel>(IStellarView<TViewModel> view)
        where TViewModel : class
    {
        lock (_bindingLock)
        {
            if (view.MaintainBindings || !_controlsBound)
            {
                return;
            }

            Volatile.Write(ref _controlsBound, false);

            ControlBindings?.Clear();

            view.UnregisterViewModelBindings();
        }
    }

    public void OnVisualElementPropertyChanged<TView, TViewModel>(TView visualElement, string viewModelPropertyName, string propertyName = null)
        where TView : VisualElement, IStellarView<TViewModel>
        where TViewModel : class
    {
        if (propertyName == VisualElement.WindowProperty.PropertyName)
        {
            if (visualElement.Window != null)
            {
                RegisterBindings(visualElement);

                OnLifecycle(LifecycleEvent.Activated);
            }
            else
            {
                OnLifecycle(LifecycleEvent.Deactivated);

                UnregisterBindings(visualElement);
            }
        }
        else if (propertyName == viewModelPropertyName)
        {
            visualElement.RegisterViewModelBindings();
        }
    }

    public void OnViewCellPropertyChanged<TView, TViewModel>(TView viewCell, string viewModelPropertyName, string propertyName = null)
        where TView : ViewCell, IStellarView<TViewModel>
        where TViewModel : class
    {
        if (propertyName == VisualElement.WindowProperty.PropertyName)
        {
            if (viewCell?.View?.GetVisualElementWindow() != null)
            {
                RegisterBindings(viewCell);

                OnLifecycle(LifecycleEvent.Activated);
            }
            else
            {
                OnLifecycle(LifecycleEvent.Deactivated);

                UnregisterBindings(viewCell);
            }
        }
        else if (propertyName == viewModelPropertyName)
        {
            viewCell.RegisterViewModelBindings();
        }
    }

    public void OnLifecycle(LifecycleEvent lifecycleEvent)
    {
        if (_lifecycle.IsValueCreated)
        {
            _lifecycle.Value.OnNext(lifecycleEvent);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                if (_lifecycle.IsValueCreated)
                {
                    _lifecycle?.Value?.Dispose();
                }

                this.ControlBindings?.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
