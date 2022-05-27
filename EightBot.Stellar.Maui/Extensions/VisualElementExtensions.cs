using EightBot.Stellar.ViewModel;

namespace EightBot.Stellar.Maui;

public static class VisualElementExtensions
{
    public static TPage GetPage<TPage>(this Element element)
        where TPage : Page, IStellarPage
    {
        return element.FindMauiContext().Services.GetService<TPage>();
    }

    public static TViewModel GetViewModel<TViewModel>(this Element element)
        where TViewModel : ViewModelBase
    {
        return element.FindMauiContext().Services.GetService<TViewModel>();
    }

    public static TService GetService<TService>(this Element element)
    {
        return element.FindMauiContext().Services.GetService<TService>();
    }

    internal static IEnumerable<Element> GetParentsPath(this Element self)
    {
        Element current = self;

        while (!current.RealParent.IsApplicationOrNull())
        {
            current = current.RealParent;
            yield return current;
        }
    }

    internal static bool IsApplicationOrNull(this Element element)
        => element == null || element is IApplication;

    internal static bool IsApplicationOrWindowOrNull(this Element element)
        => element == null || element is IApplication || element is IWindow;

    private static IMauiContext FindMauiContext(this Element element, bool fallbackToAppMauiContext = false)
    {
        if (element is IElement fe && fe.Handler?.MauiContext != null)
        {
            return fe.Handler.MauiContext;
        }

        foreach (var parent in element.GetParentsPath())
        {
            if (parent is IElement parentView && parentView.Handler?.MauiContext != null)
            {
                return parentView.Handler.MauiContext;
            }
        }

        return fallbackToAppMauiContext ? Application.Current?.FindMauiContext() : default;
    }
}