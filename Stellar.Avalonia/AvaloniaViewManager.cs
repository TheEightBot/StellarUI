using Avalonia.Controls;
using Avalonia.Threading;

namespace Stellar.Avalonia;

public class AvaloniaViewManager<TViewModel> : ViewManager<TViewModel>
    where TViewModel : class
{
    private IStellarView<TViewModel> _view;

    public override void HandleActivated(IStellarView<TViewModel> view)
    {
        base.HandleActivated(view);

        if (HotReloadService.HotReloadAware)
        {
            _view = view as IStellarView<TViewModel>;
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
        if (_view is null)
        {
            return;
        }

        Dispatcher.UIThread
            .Invoke(
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
