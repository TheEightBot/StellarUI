using ReactiveUI;
using Stellar.ViewModel;

namespace Stellar.Maui;

public class MauiViewManager<TViewModel> : ViewManager
    where TViewModel : class
{
    private WeakReference<object?>? _reloadView;

    public void OnHandlerChanged<TVisualElement>(TVisualElement visualElement, HandlerChangingEventArgs args)
        where TVisualElement : VisualElement, IStellarView<TViewModel>, IStellarView
    {
        if (args.OldHandler is not null)
        {
            visualElement.Loaded -= this.Handle_Loaded;
            visualElement.Unloaded -= this.Handle_Unloaded;

            HandleDeactivated(visualElement);
            visualElement.DisposeView();
        }

        if (args.NewHandler is not null)
        {
            HandleActivated(visualElement);

            visualElement.Loaded -= this.Handle_Loaded;
            visualElement.Loaded += this.Handle_Loaded;

            visualElement.Unloaded -= this.Handle_Unloaded;
            visualElement.Unloaded += this.Handle_Unloaded;
        }
    }

    private void Handle_Loaded(object? sender, EventArgs e)
    {
        if (HotReloadService.HotReloadAware)
        {
            _reloadView = new WeakReference<object?>(sender);
            HotReloadService.UpdateApplicationEvent -= HandleHotReload;
            HotReloadService.UpdateApplicationEvent += HandleHotReload;
        }

        if (sender is not IStellarView<TViewModel> isv)
        {
            return;
        }

        OnLifecycle(isv, LifecycleEvent.Attached);
    }

    private void Handle_Unloaded(object? sender, EventArgs e)
    {
        if (HotReloadService.HotReloadAware)
        {
            HotReloadService.UpdateApplicationEvent -= HandleHotReload;
        }

        if (sender is not IStellarView<TViewModel> isv)
        {
            return;
        }

        OnLifecycle(isv, LifecycleEvent.Detached);
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
