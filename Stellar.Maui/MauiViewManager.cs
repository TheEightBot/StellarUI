using ReactiveUI;
using Stellar.ViewModel;

namespace Stellar.Maui;

public class MauiViewManager<TViewModel> : ViewManager
    where TViewModel : class
{
    private IStellarView<TViewModel> _view;

    public void HandlerChanging(IStellarView<TViewModel> view, HandlerChangingEventArgs args)
    {
        if (args.NewHandler is not null)
        {
            if (HotReloadService.HotReloadAware)
            {
                _view = view;
                HotReloadService.UpdateApplicationEvent -= HandleHotReload;
                HotReloadService.UpdateApplicationEvent += HandleHotReload;
            }

            HandleActivated(view);

            return;
        }

        HandleDeactivated(view);

        if (HotReloadService.HotReloadAware)
        {
            _view = null;
            HotReloadService.UpdateApplicationEvent -= HandleHotReload;
        }
    }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    private void HandleHotReload(Type[]? updatedTypes)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    {
        if (_view is null)
        {
            return;
        }

        MainThread
            .BeginInvokeOnMainThread(
                () =>
                {
                    var maintainStatus = _view.ViewManager.Maintain;

                    _view.ViewManager.Maintain = false;

                    _view.ViewManager.UnregisterBindings(_view);

                    _view.ViewManager.Maintain = maintainStatus;

                    _view.SetupUserInterface();

                    _view.ViewManager.RegisterBindings(_view);
                });
    }
}
