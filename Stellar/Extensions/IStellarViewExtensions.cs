using System;
using Stellar.ViewModel;

namespace Stellar;

public static class IStellarViewExtensions
{
    public static void InitializeStellarComponent<TViewModel>(
        this IStellarView<TViewModel> stellarView,
        TViewModel viewModel = null,
        bool resolveViewModel = true,
        bool delayBindingRegistrationUntilAttached = false)
        where TViewModel : class
    {
        if (Attribute.GetCustomAttribute(stellarView.GetType(), typeof(ServiceRegistrationAttribute)) is ServiceRegistrationAttribute sra)
        {
            switch (sra.ServiceRegistrationType)
            {
                case Lifetime.Scoped:
                case Lifetime.Singleton:
                    stellarView.Maintain = true;
                    break;
            }
        }

        stellarView.SetupViewModel(viewModel, resolveViewModel);

        stellarView.Initialize();

        stellarView.SetupUserInterface();

        if (!delayBindingRegistrationUntilAttached)
        {
            stellarView.ViewManager.RegisterBindings(stellarView);
        }
    }

    public static void ManageDispose<TViewModel>(this IStellarView<TViewModel> stellarView, bool disposing, ref bool isDisposed)
        where TViewModel : class
    {
        if (!isDisposed)
        {
            isDisposed = true;

            if (disposing)
            {
                stellarView.ViewManager?.Dispose();
                stellarView.ViewModel = null;

                stellarView.DisposeViewModel();
            }
        }
    }
}