namespace Stellar.Maui;

public static class PageExtensions
{
    public static bool IsPresentedModally(this Page page)
    {
        return page != null && page.Navigation != null && page.Navigation.ModalStack.Contains(page);
    }

    public static bool IsPresentedOnStack(this Page page)
    {
        return page != null && page.Navigation != null && page.Navigation.NavigationStack.Contains(page);
    }

    public static async Task<IEnumerable<Page>> PopTo<TPage>(this VisualElement visualElement, bool animated = true)
        where TPage : Page
    {
        var navStack = visualElement?.Navigation?.NavigationStack;

        if (!navStack?.Any() ?? true)
        {
            return Enumerable.Empty<Page>();
        }

        var poppedPages = new List<Page>();

        for (int i = navStack.Count - 1; i >= 0; i--)
        {
            var currPage = navStack[i];

            if (currPage == visualElement)
            {
                continue;
            }

            poppedPages.Add(currPage);

            if (currPage is TPage)
            {
                await visualElement.Navigation.PopAsync(animated);
                break;
            }

            visualElement.Navigation.RemovePage(currPage);
        }

        return poppedPages;
    }

    public static async Task AppearingAsync(this Page page)
    {
        var tcs = new TaskCompletionSource<bool>();

        try
        {
            page.Appearing += Page_Appearing;

            await tcs.Task.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            tcs.TrySetException(ex);
        }
        finally
        {
            page.Appearing -= Page_Appearing;
        }

        void Page_Appearing(object sender, EventArgs e)
        {
            tcs.TrySetResult(true);
        }
    }

    public static async Task DisappearingAsync(this Page page)
    {
        var tcs = new TaskCompletionSource<bool>();

        try
        {
            page.Disappearing += Page_Disappearing;

            await tcs.Task.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            tcs.TrySetException(ex);
        }
        finally
        {
            page.Disappearing -= Page_Disappearing;
        }

        void Page_Disappearing(object sender, EventArgs e)
        {
            tcs.TrySetResult(true);
        }
    }
}