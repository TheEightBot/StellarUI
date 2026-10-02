using ReactiveUI;
using Stellar.ViewModel;

namespace Stellar.Maui;

public class MauiViewManager<TViewModel> : ViewManager<TViewModel>, IParkedItemView
    where TViewModel : class
{
    private WeakReference<object?>? _reloadView;

    private WeakReference<ItemsView?>? _itemsHost;

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

    private void TrackItemsHost(VisualElement view)
    {
        if (view.Parent is not ItemsView host)
        {
            this._itemsHost?.SetTarget(null);
            return;
        }

        if (this._itemsHost is not null)
        {
            this._itemsHost.SetTarget(host);
            return;
        }

        this._itemsHost = new WeakReference<ItemsView?>(host);

        ItemsHostWatcher.Find(host)?.ReleaseParked();
    }

    private bool TryPark(VisualElement element, IStellarView<TViewModel> view)
    {
        var viewModel = view.ViewModel;

        if (this.Maintain
            || viewModel is null
            || ReferenceEquals(element.BindingContext, viewModel)
            || this._itemsHost is null
            || !this._itemsHost.TryGetTarget(out var host)
            || host?.Window is null)
        {
            return false;
        }

        // A list has a watcher only once it has opted in through RecycledItemViewLimit,
        // so a list that has not costs nothing here.
        if (ItemsHostWatcher.Find(host) is not { } watcher)
        {
            return false;
        }

        var parking = this._parking ??= new Parking(this);

        if (!watcher.TryPark(parking.Owner))
        {
            return false;
        }

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

        watcher.Unpark(parking.Owner);

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

        public Parking(IParkedItemView owner)
        {
            Owner = new WeakReference<IParkedItemView>(owner);
        }

        public WeakReference<IParkedItemView> Owner { get; }

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
