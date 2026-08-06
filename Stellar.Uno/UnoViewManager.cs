using Microsoft.UI.Xaml;

namespace Stellar.Uno;

public class UnoViewManager<TViewModel> : ViewManager<TViewModel>
    where TViewModel : class
{
    // Null whenever no view is activated, which HandleDeactivated relies on.
    private IStellarView<TViewModel>? _view;

    public override void HandleActivated(IStellarView<TViewModel> view)
    {
        base.HandleActivated(view);

        if (HotReloadService.HotReloadAware)
        {
            _view = view;
            HotReloadService.UpdateApplicationEvent -= HandleHotReload;
            HotReloadService.UpdateApplicationEvent += HandleHotReload;
        }
    }

    public override void HandleDeactivated(IStellarView<TViewModel> view)
    {
        if (HotReloadService.HotReloadAware)
        {
            _view = null;
            HotReloadService.UpdateApplicationEvent -= HandleHotReload;
        }

        base.HandleDeactivated(view);
    }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    private void HandleHotReload(Type[]? updatedTypes)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    {
        if (_view is not FrameworkElement fe)
        {
            return;
        }

        fe.DispatcherQueue.TryEnqueue(
            () =>
            {
                var view = _view;

                if (view is null)
                {
                    return;
                }

                var maintainStatus = view.ViewManager.Maintain;

                view.ViewManager.Maintain = false;

                view.ViewManager.UnregisterBindings(view);

                view.ViewManager.Maintain = maintainStatus;

                view.SetupUserInterface();

                view.ViewManager.RegisterBindings(view);
            });
    }
}
