using System.Reactive.Threading.Tasks;

namespace EightBot.Stellar.Maui;

public static class NavigationObservableExtensions
{
    private static uint _navigatingCount;

    /*this is roughly 12 UI ticks at 60fps. Slightly more than 200ms*/
    public static TimeSpan DefaultMultiTapThrottleDuration { get; set; }
        = TimeSpan.FromMilliseconds(17 * 12);

    public static bool Navigating
    {
        get => Interlocked.Exchange(ref _navigatingCount, 0) > 0;

        set
        {
            if (value == true)
            {
                Interlocked.Increment(ref _navigatingCount);
            }
            else
            {
                Interlocked.Decrement(ref _navigatingCount);
            }
        }
    }

    public static IDisposable NavigateToPage<TParameter, TPage>(
        this IObservable<TParameter> observable,
        VisualElement element,
        Action<TPage, TParameter> preNavigation = null,
        Action<TPage, TParameter> postNavigation = null,
        bool animated = true,
        bool allowMultiple = false,
        IScheduler pageCreationScheduler = null,
        TimeSpan? multiTapThrottleDuration = null)
        where TPage : Page, IStellarPage
    {
        return observable
            .ThrottleFirst(multiTapThrottleDuration ?? DefaultMultiTapThrottleDuration, RxApp.TaskpoolScheduler)
            .Where(_ => allowMultiple || !Navigating)
            .Do(_ => Navigating = true)
            .ObserveOn(pageCreationScheduler ?? RxApp.TaskpoolScheduler)
            .Select(
                x =>
                {
                    var page = element.GetPage<TPage>();
                    return (Page: page, Parameter: x, IsAppearingTask: page.AppearingAsync());
                })
            .ObserveOn(RxApp.MainThreadScheduler)
            .SelectMany(
                async x =>
                {
                    preNavigation?.Invoke(x.Page, x.Parameter);

                    await Task.WhenAll(
                        x.IsAppearingTask,
                        element.Navigation.PushAsync(x.Page, animated));

                    postNavigation?.Invoke(x.Page, x.Parameter);

                    Navigating = false;

                    return Unit.Default;
                })
            .Subscribe();
    }

    public static IDisposable NavigateToPage<TParameter, TPage>(
        this IObservable<TParameter> observable,
        VisualElement element,
        Func<TParameter, TPage> pageCreator,
        Action<TPage, TParameter> preNavigation = null,
        Action<TPage, TParameter> postNavigation = null,
        bool animated = true,
        bool allowMultiple = false,
        IScheduler pageCreationScheduler = null,
        TimeSpan? multiTapThrottleDuration = null)
        where TPage : Page, IStellarPage
    {
        return observable
            .ThrottleFirst(multiTapThrottleDuration ?? DefaultMultiTapThrottleDuration, RxApp.TaskpoolScheduler)
            .Where(_ => allowMultiple || !Navigating)
            .Do(_ => Navigating = true)
            .ObserveOn(pageCreationScheduler ?? RxApp.TaskpoolScheduler)
            .Select(
                x =>
                {
                    var page = pageCreator.Invoke(x);
                    return (Page: page, Parameter: x, IsAppearingTask: page.AppearingAsync());
                })
            .ObserveOn(RxApp.MainThreadScheduler)
            .SelectMany(
                async x =>
                {
                    preNavigation?.Invoke(x.Page, x.Parameter);

                    await Task.WhenAll(
                        x.IsAppearingTask,
                        element.Navigation.PushAsync(x.Page, animated));

                    postNavigation?.Invoke(x.Page, x.Parameter);

                    Navigating = false;

                    return Unit.Default;
                })
            .Subscribe();
    }

    public static IDisposable NavigatePopTo<TParameter, TPage>(
        this IObservable<TParameter> observable,
        Page page,
        Action<TParameter> preNavigation = null,
        Action<TParameter> postNavigation = null,
        bool animated = true,
        bool allowMultiple = false,
        TimeSpan? multiTapThrottleDuration = null)
        where TPage : Page
    {
        return observable
            .ThrottleFirst(multiTapThrottleDuration ?? DefaultMultiTapThrottleDuration, RxApp.TaskpoolScheduler)
            .Where(_ => allowMultiple || !Navigating)
            .Do(_ => Navigating = true)
            .ObserveOn(RxApp.MainThreadScheduler)
            .SelectMany(
                async parameter =>
                {
                    preNavigation?.Invoke(parameter);
                    var pages = await page.PopTo<TPage>(animated);
                    postNavigation?.Invoke(parameter);

                    foreach (var page in pages)
                    {
                        page.DisposeView();
                    }

                    Navigating = false;

                    return Unit.Default;
                })
            .Subscribe();
    }

    public static IDisposable NavigatePopPage<TParameter>(
        this IObservable<TParameter> observable,
        VisualElement element,
        Action<TParameter> preNavigation = null,
        Action<TParameter> postNavigation = null,
        bool animated = true,
        bool allowMultiple = false,
        TimeSpan? multiTapThrottleDuration = null)
    {
        return observable
            .ThrottleFirst(multiTapThrottleDuration ?? DefaultMultiTapThrottleDuration, RxApp.TaskpoolScheduler)
            .Where(_ => allowMultiple || !Navigating)
            .Do(_ => Navigating = true)
            .ObserveOn(RxApp.MainThreadScheduler)
            .SelectMany(
                async parameter =>
                {
                    preNavigation?.Invoke(parameter);
                    var page = await element.Navigation.PopAsync(animated);
                    postNavigation?.Invoke(parameter);

                    page.DisposeView();

                    Navigating = false;

                    return Unit.Default;
                })
            .Subscribe();
    }

    public static IDisposable NavigatePopToRoot<TParameter>(
        this IObservable<TParameter> observable,
        VisualElement element,
        Action<TParameter> preNavigation = null,
        Action<TParameter> postNavigation = null,
        bool animated = true,
        bool allowMultiple = false,
        TimeSpan? multiTapThrottleDuration = null)
    {
        return observable
            .ThrottleFirst(multiTapThrottleDuration ?? DefaultMultiTapThrottleDuration, RxApp.TaskpoolScheduler)
            .Where(_ => allowMultiple || !Navigating)
            .Do(_ => Navigating = true)
            .ObserveOn(RxApp.MainThreadScheduler)
            .SelectMany(
                async parameter =>
                {
                    preNavigation?.Invoke(parameter);
                    await element.Navigation.PopToRootAsync(animated);
                    postNavigation?.Invoke(parameter);

                    Navigating = false;

                    return Unit.Default;
                })
            .Subscribe();
    }

    public static IDisposable NavigateToModalPage<TParameter, TPage>(
        this IObservable<TParameter> observable,
        VisualElement element,
        Action<TPage, TParameter> preNavigation = null,
        Action<TPage, TParameter> postNavigation = null,
        bool animated = true,
        bool allowMultiple = false,
        IScheduler pageCreationScheduler = null,
        TimeSpan? multiTapThrottleDuration = null)
        where TPage : Page, IStellarPage
    {
        return observable
            .ThrottleFirst(multiTapThrottleDuration ?? DefaultMultiTapThrottleDuration, RxApp.TaskpoolScheduler)
            .Where(_ => allowMultiple || !Navigating)
            .Do(_ => Navigating = true)
            .ObserveOn(pageCreationScheduler ?? RxApp.TaskpoolScheduler)
            .Select(
                x =>
                {
                    var page = element.GetPage<TPage>();
                    return (Page: page, Parameter: x, IsAppearingTask: page.AppearingAsync());
                })
            .ObserveOn(RxApp.MainThreadScheduler)
            .SelectMany(
                async x =>
                {
                    preNavigation?.Invoke(x.Page, x.Parameter);
                    await Task.WhenAll(
                        x.IsAppearingTask,
                        element.Navigation.PushModalAsync(x.Page, animated));
                    postNavigation?.Invoke(x.Page, x.Parameter);

                    Navigating = false;

                    return Unit.Default;
                })
            .Subscribe();
    }

    public static IDisposable NavigateToModalPage<TParameter, TPage>(
        this IObservable<TParameter> observable,
        VisualElement element,
        Func<TParameter, TPage> pageCreator,
        Action<TPage, TParameter> preNavigation = null,
        Action<TPage, TParameter> postNavigation = null,
        bool animated = true,
        bool allowMultiple = false,
        IScheduler pageCreationScheduler = null,
        TimeSpan? multiTapThrottleDuration = null)
        where TPage : Page, IStellarPage
    {
        return observable
            .ThrottleFirst(multiTapThrottleDuration ?? DefaultMultiTapThrottleDuration, RxApp.TaskpoolScheduler)
            .Where(_ => allowMultiple || !Navigating)
            .Do(_ => Navigating = true)
            .ObserveOn(pageCreationScheduler ?? RxApp.TaskpoolScheduler)
            .Select(
                x =>
                {
                    var page = pageCreator.Invoke(x);
                    return (Page: page, Parameter: x, IsAppearingTask: page.AppearingAsync());
                })
            .ObserveOn(RxApp.MainThreadScheduler)
            .SelectMany(
                async x =>
                {
                    preNavigation?.Invoke(x.Page, x.Parameter);
                    await Task.WhenAll(
                        x.IsAppearingTask,
                        element.Navigation.PushModalAsync(x.Page, animated));
                    postNavigation?.Invoke(x.Page, x.Parameter);

                    Navigating = false;

                    return Unit.Default;
                })
            .Subscribe();
    }

    public static IDisposable NavigatePopModalPage<TParameter>(
        this IObservable<TParameter> observable,
        VisualElement element,
        Action<TParameter> preNavigation = null,
        Action<TParameter> postNavigation = null,
        bool animated = true,
        bool allowMultiple = false,
        TimeSpan? multiTapThrottleDuration = null)
    {
        return observable
            .ThrottleFirst(multiTapThrottleDuration ?? DefaultMultiTapThrottleDuration, RxApp.TaskpoolScheduler)
            .Where(_ => allowMultiple || !Navigating)
            .Do(_ => Navigating = true)
            .ObserveOn(RxApp.MainThreadScheduler)
            .SelectMany(
                async parameter =>
                {
                    preNavigation?.Invoke(parameter);
                    var page = await element.Navigation.PopModalAsync(animated);
                    postNavigation?.Invoke(parameter);

                    page.DisposeView();

                    Navigating = false;

                    return Unit.Default;
                })
            .Subscribe();
    }
}