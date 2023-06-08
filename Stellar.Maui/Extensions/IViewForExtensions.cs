using Stellar.Exceptions;
using Stellar.ViewModel;

namespace Stellar.Maui;

public static class IViewForExtensions
{
    public static void SetupViewModel<TViewModel>(
        this IViewFor<TViewModel> view,
        TViewModel viewModel = null,
        bool resolveViewModel = true)
        where TViewModel : class
    {
        if (viewModel is not null && !viewModel.Equals(view.ViewModel))
        {
            view.ViewModel = viewModel;
        }
        else if (view.ViewModel is null && resolveViewModel && view is Element element)
        {
            var resolvedViewModel = element.GetService<TViewModel>();

            if (resolvedViewModel is null)
            {
                throw new RegisteredServiceNotFoundException($"Unable to find a registration for a ViewModel of type {typeof(TViewModel).FullName}. Verify that the [ServiceRegistration] attribute was set or that the ViewModel was registered with dependency injection.");
            }

            view.ViewModel = resolvedViewModel;
        }

        if (view.ViewModel is not null && view.ViewModel is ViewModelBase vmb)
        {
            if (Attribute.GetCustomAttribute(typeof(TViewModel), typeof(ServiceRegistrationAttribute)) is ServiceRegistrationAttribute sra)
            {
                switch (sra.ServiceRegistrationType)
                {
                    case Lifetime.Scoped:
                    case Lifetime.Singleton:
                        vmb.Maintain = true;
                        break;
                }
            }

            vmb.SetupViewModel();
        }
    }
}