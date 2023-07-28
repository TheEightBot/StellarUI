using ReactiveUI;
using Stellar.ViewModel;

namespace Stellar.Maui;

public class MauiViewManager : ViewManager
{
#if DEBUG
    private IStellarView? _view;
#endif

    public void HandlerChanging<TViewModel>(IStellarView<TViewModel> view, HandlerChangingEventArgs args)
        where TViewModel : class
    {
        if (args.NewHandler is not null)
        {
#if DEBUG
            _view = view;
            HotReloadService.UpdateApplicationEvent -= HandleHotReload;
            HotReloadService.UpdateApplicationEvent += HandleHotReload;
#endif

            HandleActivated(view);

            return;
        }

        HandleDeactivated(view);
#if DEBUG
        HotReloadService.UpdateApplicationEvent -= HandleHotReload;
#endif
    }

#if DEBUG
    private void HandleHotReload(Type[] updatedTypes)
    {
        if (_view is null)
        {
            return;
        }

        _view.SetupUserInterface();
    }
#endif
}
