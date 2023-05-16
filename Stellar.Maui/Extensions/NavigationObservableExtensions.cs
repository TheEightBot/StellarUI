using System.Reactive.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace Stellar.Maui;

public static class NavigationObservableExtensions
{
    private static uint _navigatingCount;

    /* This is roughly 12 UI ticks at 60fps. Slightly more than 200ms */
    public static TimeSpan DefaultMultiTapThrottleDuration { get; set; }
        = TimeSpan.FromMilliseconds(17 * 12);

    public static bool Navigating
    {
        get => Interlocked.Exchange(ref _navigatingCount, 0) > 0;

        set
        {
            if (value)
            {
                Interlocked.Increment(ref _navigatingCount);
                return;
            }

            Interlocked.Decrement(ref _navigatingCount);
        }
    }

    public static IDisposable NavigateToPage<TPage>(
        this IObservable<Unit> observable,
        VisualElement element,
        Action<TPage, Unit> preNavigation = null,
        Action<TPage, Unit> postNavigation = null,
        bool animated = true,
        bool allowMultiple = false,
        IScheduler pageCreationScheduler = null,
        TimeSpan? multiTapThrottleDuration = null)
        where TPage : Page
    {
        return NavigateToPage<Unit, TPage>(observable, element, preNavigation, postNavigation, animated, allowMultiple, pageCreationScheduler, multiTapThrottleDuration);
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
        where TPage : Page
    {
        return observable
            .ThrottleFirst(multiTapThrottleDuration ?? DefaultMultiTapThrottleDuration, Schedulers.ShortTermThreadPoolScheduler)
            .Where(_ => allowMultiple || !Navigating)
            .Do(static _ => Navigating = true)
            .ObserveOn(pageCreationScheduler ?? Schedulers.ShortTermThreadPoolScheduler)
            .Select(
                x =>
                {
                    var page = element.GetPage<TPage>();
                    return new NavigationOptions<TPage, TParameter>
                    {
                        Page = page,
                        Parameter = x,
                        IsAppearingAsync = page.AppearingAsync(),
                        PreNavigation = preNavigation,
                        PostNavigation = postNavigation,
                        Animated = animated,
                        NavigationRoot = element,
                    };
                })
            .ObserveOn(RxApp.MainThreadScheduler)
            .SelectMany(
                static async x =>
                {
                    try
                    {
                        x.PreNavigation?.Invoke(x.Page, x.Parameter);

                        await Task.WhenAll(
                            x.IsAppearingAsync,
                            x.NavigationRoot.Navigation.PushAsync(x.Page, x.Animated));

                        x.PostNavigation?.Invoke(x.Page, x.Parameter);
                    }
                    finally
                    {
                        Navigating = false;
                    }

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
        where TPage : Page
    {
        return observable
            .ThrottleFirst(multiTapThrottleDuration ?? DefaultMultiTapThrottleDuration, Schedulers.ShortTermThreadPoolScheduler)
            .Where(_ => allowMultiple || !Navigating)
            .Do(static _ => Navigating = true)
            .ObserveOn(pageCreationScheduler ?? Schedulers.ShortTermThreadPoolScheduler)
            .Select(
                x =>
                {
                    var page = pageCreator.Invoke(x);
                    return new NavigationOptions<TPage, TParameter>
                    {
                        Page = page,
                        Parameter = x,
                        IsAppearingAsync = page.AppearingAsync(),
                        PreNavigation = preNavigation,
                        PostNavigation = postNavigation,
                        Animated = animated,
                        NavigationRoot = element,
                    };
                })
            .ObserveOn(RxApp.MainThreadScheduler)
            .SelectMany(
                static async x =>
                {
                    try
                    {
                        x.PreNavigation?.Invoke(x.Page, x.Parameter);

                        await Task.WhenAll(
                            x.IsAppearingAsync,
                            x.NavigationRoot.Navigation.PushAsync(x.Page, x.Animated));

                        x.PostNavigation?.Invoke(x.Page, x.Parameter);
                    }
                    finally
                    {
                        Navigating = false;
                    }

                    return Unit.Default;
                })
            .Subscribe();
    }

    public static IDisposable NavigatePopTo<TParameter, TPage>(
        this IObservable<TParameter> observable,
        VisualElement element,
        Action<TParameter> preNavigation = null,
        Action<TParameter> postNavigation = null,
        bool animated = true,
        bool allowMultiple = false,
        TimeSpan? multiTapThrottleDuration = null)
        where TPage : Page
    {
        return observable
            .ThrottleFirst(multiTapThrottleDuration ?? DefaultMultiTapThrottleDuration, Schedulers.ShortTermThreadPoolScheduler)
            .Where(_ => allowMultiple || !Navigating)
            .Do(static _ => Navigating = true)
            .Select(
                x =>
                {
                    return new NavigationOptions<TParameter>
                    {
                        Parameter = x,
                        PreNavigation = preNavigation,
                        PostNavigation = postNavigation,
                        Animated = animated,
                        NavigationRoot = element,
                    };
                })
            .ObserveOn(RxApp.MainThreadScheduler)
            .SelectMany(
                static async x =>
                {
                    try
                    {
                        x.PreNavigation?.Invoke(x.Parameter);
                        var pages = await x.NavigationRoot.PopTo<TPage>(x.Animated);
                        x.PostNavigation?.Invoke(x.Parameter);
                    }
                    finally
                    {
                        Navigating = false;
                    }

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
            .ThrottleFirst(multiTapThrottleDuration ?? DefaultMultiTapThrottleDuration, Schedulers.ShortTermThreadPoolScheduler)
            .Where(_ => allowMultiple || !Navigating)
            .Do(static _ => Navigating = true)
            .Select(
                x =>
                {
                    return new NavigationOptions<TParameter>
                    {
                        Parameter = x,
                        PreNavigation = preNavigation,
                        PostNavigation = postNavigation,
                        Animated = animated,
                        NavigationRoot = element,
                    };
                })
            .ObserveOn(RxApp.MainThreadScheduler)
            .SelectMany(
                static async x =>
                {
                    try
                    {
                        x.PreNavigation?.Invoke(x.Parameter);
                        var page = await x.NavigationRoot.Navigation.PopAsync(x.Animated);
                        x.PostNavigation?.Invoke(x.Parameter);
                    }
                    finally
                    {
                        Navigating = false;
                    }

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
            .ThrottleFirst(multiTapThrottleDuration ?? DefaultMultiTapThrottleDuration, Schedulers.ShortTermThreadPoolScheduler)
            .Where(_ => allowMultiple || !Navigating)
            .Do(static _ => Navigating = true)
            .Select(
                x =>
                {
                    return new NavigationOptions<TParameter>
                    {
                        Parameter = x,
                        PreNavigation = preNavigation,
                        PostNavigation = postNavigation,
                        Animated = animated,
                        NavigationRoot = element,
                    };
                })
            .ObserveOn(RxApp.MainThreadScheduler)
            .SelectMany(
                static async x =>
                {
                    try
                    {
                        x.PreNavigation?.Invoke(x.Parameter);
                        await x.NavigationRoot.Navigation.PopToRootAsync(x.Animated);
                        x.PostNavigation?.Invoke(x.Parameter);
                    }
                    finally
                    {
                        Navigating = false;
                    }

                    return Unit.Default;
                })
            .Subscribe();
    }

    public static IDisposable NavigateToModalPage<TPage>(
        this IObservable<Unit> observable,
        VisualElement element,
        Action<TPage, Unit> preNavigation = null,
        Action<TPage, Unit> postNavigation = null,
        bool animated = true,
        bool allowMultiple = false,
        IScheduler pageCreationScheduler = null,
        TimeSpan? multiTapThrottleDuration = null)
        where TPage : Page
    {
        return NavigateToModalPage<Unit, TPage>(observable, element, preNavigation, postNavigation, animated, allowMultiple, pageCreationScheduler, multiTapThrottleDuration);
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
        where TPage : Page
    {
        return observable
            .ThrottleFirst(multiTapThrottleDuration ?? DefaultMultiTapThrottleDuration, Schedulers.ShortTermThreadPoolScheduler)
            .Where(_ => allowMultiple || !Navigating)
            .Do(static _ => Navigating = true)
            .ObserveOn(pageCreationScheduler ?? Schedulers.ShortTermThreadPoolScheduler)
            .Select(
                x =>
                {
                    var page = element.GetPage<TPage>();
                    return new NavigationOptions<TPage, TParameter>
                    {
                        Page = page,
                        Parameter = x,
                        IsAppearingAsync = page.AppearingAsync(),
                        PreNavigation = preNavigation,
                        PostNavigation = postNavigation,
                        Animated = animated,
                        NavigationRoot = element,
                    };
                })
            .ObserveOn(RxApp.MainThreadScheduler)
            .SelectMany(
                static async x =>
                {
                    try
                    {
                        x.PreNavigation?.Invoke(x.Page, x.Parameter);
                        await Task.WhenAll(
                            x.IsAppearingAsync,
                            x.NavigationRoot.Navigation.PushModalAsync(x.Page, x.Animated));
                        x.PostNavigation?.Invoke(x.Page, x.Parameter);
                    }
                    finally
                    {
                        Navigating = false;
                    }

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
        where TPage : Page
    {
        return observable
            .ThrottleFirst(multiTapThrottleDuration ?? DefaultMultiTapThrottleDuration, Schedulers.ShortTermThreadPoolScheduler)
            .Where(_ => allowMultiple || !Navigating)
            .Do(static _ => Navigating = true)
            .ObserveOn(pageCreationScheduler ?? Schedulers.ShortTermThreadPoolScheduler)
            .Select(
                x =>
                {
                    var page = pageCreator.Invoke(x);
                    return new NavigationOptions<TPage, TParameter>
                    {
                        Page = page,
                        Parameter = x,
                        IsAppearingAsync = page.AppearingAsync(),
                        PreNavigation = preNavigation,
                        PostNavigation = postNavigation,
                        Animated = animated,
                        NavigationRoot = element,
                    };
                })
            .ObserveOn(RxApp.MainThreadScheduler)
            .SelectMany(
                static async x =>
                {
                    try
                    {
                        x.PreNavigation?.Invoke(x.Page, x.Parameter);
                        await Task.WhenAll(
                            x.IsAppearingAsync,
                            x.NavigationRoot.Navigation.PushModalAsync(x.Page, x.Animated));
                        x.PostNavigation?.Invoke(x.Page, x.Parameter);
                    }
                    finally
                    {
                        Navigating = false;
                    }

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
            .ThrottleFirst(multiTapThrottleDuration ?? DefaultMultiTapThrottleDuration, Schedulers.ShortTermThreadPoolScheduler)
            .Where(_ => allowMultiple || !Navigating)
            .Do(static _ => Navigating = true)
            .Select(
                x =>
                {
                    return new NavigationOptions<TParameter>
                    {
                        Parameter = x,
                        PreNavigation = preNavigation,
                        PostNavigation = postNavigation,
                        Animated = animated,
                        NavigationRoot = element,
                    };
                })
            .ObserveOn(RxApp.MainThreadScheduler)
            .SelectMany(
                static async x =>
                {
                    try
                    {
                        x.PreNavigation?.Invoke(x.Parameter);
                        var page = await x.NavigationRoot.Navigation.PopModalAsync(x.Animated);
                        x.PostNavigation?.Invoke(x.Parameter);
                    }
                    finally
                    {
                        Navigating = false;
                    }

                    return Unit.Default;
                })
            .Subscribe();
    }

    public record NavigationOptions<TPage, TParameter>
        : NavigationOptions<TParameter>
        where TPage : Page
    {
        public TPage Page { get; set; }

        public Task IsAppearingAsync { get; set; }

        public new Action<TPage, TParameter> PreNavigation { get; set; }

        public new Action<TPage, TParameter> PostNavigation { get; set; }
    }

    public record NavigationOptions<TParameter>
    {
        public TParameter Parameter { get; set; }

        public Action<TParameter> PreNavigation { get; set; }

        public Action<TParameter> PostNavigation { get; set; }

        public bool Animated { get; set; }

        public VisualElement NavigationRoot { get; set; }
    }
}