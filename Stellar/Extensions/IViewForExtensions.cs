using Splat;
using Stellar.Exceptions;
using Stellar.ViewModel;

namespace Stellar;

public static class IViewForExtensions
{
    public static void SetupViewModel<TViewModel>(
        this IViewFor<TViewModel> view,
        TViewModel? viewModel = null,
        bool resolveViewModel = true)
        where TViewModel : class
    {
        if (viewModel is not null && !viewModel.Equals(view.ViewModel))
        {
            view.ViewModel = viewModel;
        }
        else if (view.ViewModel is null && resolveViewModel)
        {
            var resolvedViewModel = Locator.Current.GetService<TViewModel>();

            if (resolvedViewModel is null)
            {
                throw new RegisteredServiceNotFoundException($"Unable to find a registration for a ViewModel of type {typeof(TViewModel).FullName}. Verify that the [ServiceRegistration] attribute was set or that the ViewModel was registered with dependency injection.");
            }

            view.ViewModel = resolvedViewModel;
        }

        if (view.ViewModel is ViewModelBase vmb)
        {
            vmb.SetupViewModel();
        }
    }

    public static void RegisterViewModelBindings<TViewModel>(this IViewFor<TViewModel> view)
            where TViewModel : class
    {
        if (view.ViewModel is ViewModelBase vmb)
        {
            vmb.Register();
        }
    }

    public static void UnregisterViewModelBindings<TViewModel>(this IViewFor<TViewModel> view)
            where TViewModel : class
    {
        if (view.ViewModel is ViewModelBase vmb)
        {
            vmb.Unregister();
        }
    }

    public static void DisposeViewModel<TViewModel>(this IViewFor<TViewModel> view)
        where TViewModel : class
    {
        if (view.ViewModel is ViewModelBase vmb && !vmb.Maintain && vmb is IDisposable id)
        {
            id.Dispose();
        }

        view.ViewModel = null;
    }

    public static void DisposeView<TViewModel>(this IStellarView<TViewModel> ve)
        where TViewModel : class
    {
        if (ve is not null && ve is IStellarView<TViewModel> isv && !isv.ViewManager.Maintain && isv is IDisposable id)
        {
            id.Dispose();
        }
    }
}
