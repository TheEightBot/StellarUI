using System.Runtime.CompilerServices;
using DynamicData.Diagnostics;
using Stellar.Exceptions;
using Stellar.ViewModel;

namespace Stellar.Maui;

public static class VisualElementExtensions
{
    public static T ThrowIfNull<T>(this T obj)
    {
        if (obj is null)
        {
            throw new RegisteredServiceNotFoundException(
                $@"""{typeof(T).Name} must be registered as a service in order to be resolved.
                    Please use the ServiceRegistration attribute or register the component with the MauiAppBuilder.");
        }

        return obj;
    }

    public static void ReloadView<TViewModel>(this IStellarView<TViewModel> view)
        where TViewModel : class
    {
        if (!HotReloadService.HotReloadAware)
        {
            return;
        }

        MainThread
            .BeginInvokeOnMainThread(
                () =>
                {
                    var maintainStatus = view.ViewManager.Maintain;

                    view.ViewManager.Maintain = false;

                    view.ViewManager.UnregisterBindings(view);

                    view.ViewManager.Maintain = maintainStatus;

                    view.SetupUserInterface();

                    view.ViewManager.RegisterBindings(view);
                });
    }
}
