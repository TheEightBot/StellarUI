using ReactiveUI;
using Stellar.ViewModel;

namespace Stellar.Maui;

public class MauiViewManager<TViewModel> : ViewManager<TViewModel>, IParkedItemView
    where TViewModel : class
{
    private WeakReference<object?>? _reloadView;

    // The watcher of the list the view belongs to; None for a list that has not opted in or
    // a parent that is not a list; null until the view has been in a list at all.
    private ItemsHostWatcher? _itemsHost;

    private Parking? _parking;

    public override void PropertyChanged<TView>(TView view, string? propertyName = null)
    {
        base.PropertyChanged(view, propertyName);

        if (propertyName is null || view is not VisualElement ve)
        {
            return;
        }

        if (propertyName.Equals(nameof(VisualElement.Parent)))
        {
            this.TrackItemsHost(ve);
            return;
        }

        if (!propertyName.Equals(nameof(VisualElement.Window)))
        {
            return;
        }

        if (ve.Window is not null)
        {
            if (HotReloadService.HotReloadAware)
            {
                this._reloadView = new WeakReference<object?>(view);
                HotReloadService.UpdateApplicationEvent -= this.HandleHotReload;
                HotReloadService.UpdateApplicationEvent += this.HandleHotReload;
            }

            if (view is not IStellarView<TViewModel> isv)
            {
                return;
            }

            this.TrackItemsHost(ve);

            if (this.Unpark(out var parkedViewModel))
            {
                if (parkedViewModel is not null && ReferenceEquals(parkedViewModel, isv.ViewModel))
                {
                    isv.RegisterViewModelBindings();
                    this.RegisterBindings(isv);
                    this.OnLifecycle(isv, LifecycleEvent.Attached);
                    return;
                }

                this.Deactivate(isv, parkedViewModel);
            }

            this.HandleActivated(isv);
            this.OnLifecycle(isv, LifecycleEvent.Attached);
        }
        else
        {
            if (HotReloadService.HotReloadAware)
            {
                HotReloadService.UpdateApplicationEvent -= this.HandleHotReload;
            }

            if (view is not IStellarView<TViewModel> isv)
            {
                return;
            }

            this.OnLifecycle(isv, LifecycleEvent.Detached);

            if (this.TryPark(ve, isv))
            {
                return;
            }

            this.Deactivate(isv);
        }
    }

    void IParkedItemView.DeactivateParked()
    {
        var view = this._parking?.View;

        if (this.Unpark(out var parkedViewModel) && view is not null)
        {
            this.Deactivate(view, parkedViewModel);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.Unpark(out _);
        }

        base.Dispose(disposing);
    }

    // Runs when the view gains a window and when its parent changes. By the time the view
    // is told its window is gone, MAUI has already cleared its parent (Element.SetParent
    // sets RealParent before OnParentSet propagates the window), so which list the view
    // is leaving has to be remembered from here. The watcher is remembered rather than
    // the list: it holds the list weakly itself, so this costs an item view nothing.
    private void TrackItemsHost(VisualElement view)
    {
        if (view.Parent is not ItemsView host)
        {
            // A view just recycled has no parent and no window, and keeps its list until
            // it is given a parent or a window again. Null is kept for a view that has
            // never had a list, which is what releases the views a recycler dropped.
            if (this._itemsHost is not null && (view.Parent is not null || view.Window is not null))
            {
                this._itemsHost = ItemsHostWatcher.None;
            }

            return;
        }

        var tracked = this._itemsHost;

        if (tracked is not null && tracked.IsFor(host))
        {
            return;
        }

        // A list has a watcher only once it has opted in through RecycledItemViewLimit.
        // For one that has not, this lookup on bind is the whole cost, and None stands in.
        var watcher = ItemsHostWatcher.Find(host);

        this._itemsHost = watcher ?? ItemsHostWatcher.None;

        if (tracked is null)
        {
            watcher?.ReleaseParked();
        }
    }

    private bool TryPark(VisualElement element, IStellarView<TViewModel> view)
    {
        var viewModel = view.ViewModel;
        var watcher = this._itemsHost;

        if (this.Maintain
            || viewModel is null
            || watcher is null
            || ReferenceEquals(element.BindingContext, viewModel))
        {
            return false;
        }

        // Nothing is allocated until the list has taken the view: the first time, the
        // watcher creates the slot's weak reference, and the parking is built around it.
        var parking = this._parking;
        var slot = parking?.Slot;

        if (!watcher.TryPark(this, ref slot))
        {
            return false;
        }

        parking ??= this._parking = new Parking(slot!);

        parking.Park(watcher, view, viewModel);

        return true;
    }

    private bool Unpark(out TViewModel? parkedViewModel)
    {
        var parking = this._parking;
        var watcher = parking?.Host;

        if (parking is null || watcher is null)
        {
            parkedViewModel = null;
            return false;
        }

        watcher.Unpark(parking.Slot);

        parkedViewModel = parking.Unpark();

        return true;
    }

    private void Deactivate(IStellarView<TViewModel> view, TViewModel? parkedViewModel = null)
    {
        this.HandleDeactivated(view);

        if (parkedViewModel is ViewModelBase replaced && !ReferenceEquals(replaced, view.ViewModel))
        {
            replaced.Unregister();
        }

        view.DisposeView();
    }

    private void HandleHotReload(Type[]? updatedTypes)
    {
        if (_reloadView is null || _reloadView.TryGetTarget(out var target) || target is not IStellarView<TViewModel> isv)
        {
            return;
        }

        isv.ReloadView();
    }

    private sealed class Parking
    {
        private readonly WeakReference<IStellarView<TViewModel>?> _view = new(null);

        private readonly WeakReference<TViewModel?> _viewModel = new(null);

        public Parking(WeakReference<IParkedItemView> slot)
        {
            Slot = slot;
        }

        /// <summary>
        /// Gets the weak reference the watcher holds this view's manager through. The same
        /// one is handed to the watcher each time, which is how it finds the slot to free.
        /// </summary>
        public WeakReference<IParkedItemView> Slot { get; }

        public ItemsHostWatcher? Host { get; private set; }

        public IStellarView<TViewModel>? View => _view.TryGetTarget(out var view) ? view : null;

        public void Park(ItemsHostWatcher host, IStellarView<TViewModel> view, TViewModel viewModel)
        {
            _view.SetTarget(view);
            _viewModel.SetTarget(viewModel);

            Host = host;
        }

        public TViewModel? Unpark()
        {
            _viewModel.TryGetTarget(out var viewModel);

            _view.SetTarget(null);
            _viewModel.SetTarget(null);

            Host = null;

            return viewModel;
        }
    }
}
