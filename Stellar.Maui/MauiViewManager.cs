using ReactiveUI;
using Stellar.ViewModel;

namespace Stellar.Maui;

public class MauiViewManager<TViewModel> : ViewManager
    where TViewModel : class
{
    private WeakReference<object?>? _reloadView;

    public override void PropertyChanged<TView, TViewModel>(TView view, string? propertyName = null)
    {
        base.PropertyChanged<TView, TViewModel>(view, propertyName);

        if (propertyName is null || !propertyName.Equals(nameof(VisualElement.Window)) || view is not VisualElement ve)
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
