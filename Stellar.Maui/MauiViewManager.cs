using ReactiveUI;
using Stellar.ViewModel;

namespace Stellar.Maui;

public class MauiViewManager<TViewModel> : ViewManager<TViewModel>
    where TViewModel : class
{
    private WeakReference<object?>? _reloadView;
    private Action<Type[]?>? _cachedHotReloadHandler;

    public override void PropertyChanged<TView>(TView view, string? propertyName = null)
    {
        base.PropertyChanged(view, propertyName);

        if (propertyName is null || !propertyName.Equals(nameof(VisualElement.Window)) || view is not VisualElement ve)
        {
            return;
        }

        if (ve.Window is not null)
        {
            if (HotReloadService.HotReloadAware)
            {
                this._reloadView = new WeakReference<object?>(view);
                _cachedHotReloadHandler ??= this.HandleHotReload;
                HotReloadService.UpdateApplicationEvent -= _cachedHotReloadHandler;
                HotReloadService.UpdateApplicationEvent += _cachedHotReloadHandler;
            }

            if (view is not IStellarView<TViewModel> isv)
            {
                return;
            }

            this.HandleActivated(isv);
            this.OnLifecycle(isv, LifecycleEvent.Attached);
        }
        else
        {
            if (HotReloadService.HotReloadAware && _cachedHotReloadHandler is not null)
            {
                HotReloadService.UpdateApplicationEvent -= _cachedHotReloadHandler;
            }

            if (view is not IStellarView<TViewModel> isv)
            {
                return;
            }

            this.OnLifecycle(isv, LifecycleEvent.Detached);
            this.HandleDeactivated(isv);
            isv.DisposeView();
        }
    }

    private void HandleHotReload(Type[]? updatedTypes)
    {
        if (_reloadView is null || _reloadView.TryGetTarget(out var target) || target is not IStellarView<TViewModel> isv)
        {
            return;
        }

        isv.ReloadView();
    }
}
