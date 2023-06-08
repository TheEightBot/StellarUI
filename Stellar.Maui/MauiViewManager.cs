using ReactiveUI;

namespace Stellar.Maui;

public class MauiViewManager : ViewManager
{
    public void HandlerChanging<TViewModel>(IStellarView<TViewModel> view, HandlerChangingEventArgs args)
        where TViewModel : class
    {
        if (args.NewHandler is not null)
        {
            HandleActivated(view);

            return;
        }

        HandleDeactivated(view);
    }
}
