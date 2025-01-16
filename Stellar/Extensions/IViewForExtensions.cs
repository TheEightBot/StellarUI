using Splat;
using Stellar.Exceptions;
using Stellar.ViewModel;

namespace Stellar;

public static class IViewForExtensions
{
    public static void SetupViewModel<TViewModel>(
        this IViewFor<TViewModel> view,
        TViewModel? viewModel)
        where TViewModel : class
    {
        if (viewModel?.Equals(view.ViewModel) == false)
        {
            view.ViewModel = viewModel;
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
        if (!ve.ViewManager.Maintain && ve is IDisposable id)
        {
            id.Dispose();
        }
    }
}
