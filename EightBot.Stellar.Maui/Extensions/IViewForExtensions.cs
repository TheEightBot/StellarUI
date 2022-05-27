using EightBot.Stellar.ViewModel;

namespace EightBot.Stellar.Maui;

public static class IViewForExtensions
{
    public static void RegisterViewModelBindings<TViewModel>(this IViewFor<TViewModel> view)
            where TViewModel : class
    {
        if (view.ViewModel is not null && view.ViewModel is ViewModelBase vmb)
        {
            vmb.RegisterBindings();
        }
    }

    public static void UnregisterViewModelBindings<TViewModel>(this IViewFor<TViewModel> view)
            where TViewModel : class
    {
        if (view.ViewModel is not null && view.ViewModel is ViewModelBase vmb)
        {
            vmb.UnregisterBindings();
        }
    }

    public static void DisposeViewModel<TViewModel>(this IViewFor<TViewModel> view)
            where TViewModel : class
    {
        if (view.ViewModel is not null && view.ViewModel is ViewModelBase vmb && !vmb.MaintainBindings)
        {
            vmb.Dispose();
        }
    }

    public static void DisposeView(this VisualElement ve)
    {
        if (ve is not null && ve is IStellarView isv && !isv.MaintainBindings)
        {
            isv.Dispose();
        }
    }
}