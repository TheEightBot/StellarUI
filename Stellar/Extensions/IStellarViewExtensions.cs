using System;
using Stellar.ViewModel;

namespace Stellar;

public static class IStellarViewExtensions
{
    public static void InitializeStellarComponent<TViewModel>(
        this IStellarView<TViewModel> stellarView,
        bool maintain = false,
        bool delayBindingRegistrationUntilAttached = false)
        where TViewModel : class
    {
        InitializeStellarComponent(stellarView, null, maintain, delayBindingRegistrationUntilAttached);
    }

    public static void InitializeStellarComponent<TViewModel>(
        this IStellarView<TViewModel> stellarView,
        TViewModel? viewModel,
        bool maintain = false,
        bool delayBindingRegistrationUntilAttached = false)
        where TViewModel : class
    {
        if (maintain)
        {
            stellarView.ViewManager.Maintain = true;
        }
        else if (Attribute.GetCustomAttribute(stellarView.GetType(), typeof(ServiceRegistrationAttribute)) is ServiceRegistrationAttribute sra)
        {
            switch (sra.ServiceRegistrationType)
            {
                case Lifetime.Scoped:
                case Lifetime.Singleton:
                    stellarView.ViewManager.Maintain = true;
                    break;
            }
        }

        stellarView.SetupViewModel(viewModel);

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
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;

        if (!disposing)
        {
            return;
        }

        stellarView.ViewManager.OnLifecycle(stellarView, LifecycleEvent.Disposed);

        stellarView.DisposeViewModel();
    }
}
