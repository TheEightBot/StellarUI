using System.Runtime.CompilerServices;
using DynamicData.Diagnostics;
using Stellar.Maui.Exceptions;
using Stellar.ViewModel;

namespace Stellar.Maui;

public static class VisualElementExtensions
{
    public static TPage GetPage<TPage>(this Element element)
        where TPage : Page
    {
        return ServiceProvider.GetService<TPage>().ThrowIfNull();
    }

    public static TViewModel GetViewModel<TViewModel>(this Element element)
        where TViewModel : ViewModelBase
    {
        return ServiceProvider.GetService<TViewModel>().ThrowIfNull();
    }

    public static TService GetService<TService>(this Element element)
    {
        return ServiceProvider.GetService<TService>().ThrowIfNull();
    }

    private static T ThrowIfNull<T>(this T obj)
    {
        if (obj is null)
        {
            throw new RegisteredServiceNotFoundException(
                $@"""{typeof(T).Name} must be registered as a service in order to be resolved.
                    Please use the ServiceRegistration attribute or register the component with the MauiAppBuilder.");
        }

        return obj;
    }
}