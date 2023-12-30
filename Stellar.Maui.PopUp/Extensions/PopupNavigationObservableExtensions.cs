using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Microsoft.Maui.Controls;
using Mopups.Pages;
using Mopups.Services;
using ReactiveUI;
using static Stellar.Maui.NavigationObservableExtensions;

namespace Stellar.Maui;

public static class PopupNavigationObservableExtensions
{
    public static IDisposable NavigateToPopupPage<TPage>(
        this IObservable<Unit> observable,
        Action<TPage, Unit> preNavigation = null,
        Action<TPage, Unit> postNavigation = null,
        Action<Unit, IDictionary<string, object>> queryParameters = null,
        bool animated = true,
        bool allowMultiple = false,
        IScheduler pageCreationScheduler = null,
        TimeSpan? multiTapThrottleDuration = null)
        where TPage : PopupPage
    {
        return NavigateToPopupPage<Unit, TPage>(observable, preNavigation, postNavigation, queryParameters, animated, allowMultiple, pageCreationScheduler, multiTapThrottleDuration);
    }

    public static IDisposable NavigateToPopupPage<TParameter, TPage>(
        this IObservable<TParameter> observable,
        Action<TPage, TParameter> preNavigation = null,
        Action<TPage, TParameter> postNavigation = null,
        Action<TParameter, IDictionary<string, object>> queryParameters = null,
        bool animated = true,
        bool allowMultiple = false,
        IScheduler pageCreationScheduler = null,
        TimeSpan? multiTapThrottleDuration = null)
        where TPage : PopupPage
    {
        return observable
            .ThrottleFirst(multiTapThrottleDuration ?? DefaultMultiTapThrottleDuration, Schedulers.ShortTermThreadPoolScheduler)
            .Where(_ => allowMultiple || !Navigating)
            .Do(static _ => Navigating = true)
            .ObserveOn(pageCreationScheduler ?? Schedulers.ShortTermThreadPoolScheduler)
            .Select(
                x =>
                {
                    var page = IPlatformApplication.Current?.Services.GetService<TPage>().ThrowIfNull();

                    return new NavigationOptions<TPage, TParameter>
                    {
                        Page = page,
                        Parameter = x,
                        IsAppearingAsync = page.AppearingAsync(),
                        QueryParameters = queryParameters,
                        PreNavigation = preNavigation,
                        PostNavigation = postNavigation,
                        Animated = animated,
                    };
                })
            .ObserveOn(RxApp.MainThreadScheduler)
            .SelectMany(
                static async x =>
                {
                    try
                    {
                        if (x.QueryParameters is not null)
                        {
                            var parameters = new Dictionary<string, object>();
                            x.QueryParameters.Invoke(x.Parameter, parameters);
                            SetViewModelParameters(x.Page, parameters);
                        }

                        x.PreNavigation?.Invoke(x.Page, x.Parameter);

                        var nav = MopupService.Instance;
                        await Task.WhenAll(
                            x.IsAppearingAsync,
                            nav.PushAsync(x.Page, x.Animated));

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

    public static IDisposable NavigateToPopupPage<TParameter, TPage>(
        this IObservable<TParameter> observable,
        Func<TParameter, TPage> pageCreator,
        Action<TPage, TParameter> preNavigation = null,
        Action<TPage, TParameter> postNavigation = null,
        Action<TParameter, IDictionary<string, object>> queryParameters = null,
        bool animated = true,
        bool allowMultiple = false,
        IScheduler pageCreationScheduler = null,
        TimeSpan? multiTapThrottleDuration = null)
        where TPage : PopupPage
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
                        QueryParameters = queryParameters,
                        PreNavigation = preNavigation,
                        PostNavigation = postNavigation,
                        Animated = animated,
                    };
                })
            .ObserveOn(RxApp.MainThreadScheduler)
            .SelectMany(
                static async x =>
                {
                    try
                    {
                        if (x.QueryParameters is not null)
                        {
                            var parameters = new Dictionary<string, object>();
                            x.QueryParameters.Invoke(x.Parameter, parameters);
                            SetViewModelParameters(x.Page, parameters);
                        }

                        x.PreNavigation?.Invoke(x.Page, x.Parameter);

                        var nav = MopupService.Instance;
                        await Task.WhenAll(
                            x.IsAppearingAsync,
                            nav.PushAsync(x.Page, x.Animated));

                        x.PostNavigation?.Invoke(x.Page, x.Parameter);
                    }
                    finally
                    {
                        Navigating = false;
                    }

                    return Unit.Default;
                })
            .Do(_ => Navigating = false)
            .Subscribe();
    }

    public static IDisposable NavigateToPopupPage<TParameter, TPage, TViewModel>(
        this IObservable<TParameter> observable,
        Func<TParameter, TPage> pageCreator,
        Action<TPage, TParameter> preNavigation = null,
        Action<TPage, TParameter> postNavigation = null,
        Action<TParameter, TViewModel> viewModelMap = null,
        bool animated = true,
        bool allowMultiple = false,
        IScheduler pageCreationScheduler = null,
        TimeSpan? multiTapThrottleDuration = null)
        where TPage : PopupPage, IViewFor<TViewModel>
        where TViewModel : class
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

                    return new NavigationOptions<TPage, TParameter, TViewModel>
                    {
                        Page = page,
                        Parameter = x,
                        IsAppearingAsync = page.AppearingAsync(),
                        ViewModelMap = viewModelMap,
                        PreNavigation = preNavigation,
                        PostNavigation = postNavigation,
                        Animated = animated,
                    };
                })
            .ObserveOn(RxApp.MainThreadScheduler)
            .SelectMany(
                static async x =>
                {
                    try
                    {
                        if (x.ViewModelMap is not null && x.Page.ViewModel is not null)
                        {
                            x.ViewModelMap.Invoke(x.Parameter, x.Page.ViewModel);
                        }

                        x.PreNavigation?.Invoke(x.Page, x.Parameter);

                        var nav = MopupService.Instance;
                        await Task.WhenAll(
                            x.IsAppearingAsync,
                            nav.PushAsync(x.Page, x.Animated));

                        x.PostNavigation?.Invoke(x.Page, x.Parameter);
                    }
                    finally
                    {
                        Navigating = false;
                    }

                    return Unit.Default;
                })
            .Do(_ => Navigating = false)
            .Subscribe();
    }

    public static IDisposable NavigatePopPopupPage<TParameter>(
        this IObservable<TParameter> observable,
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
                    };
                })
            .ObserveOn(RxApp.MainThreadScheduler)
            .SelectMany(
                static async x =>
                {
                    try
                    {
                        x.PreNavigation?.Invoke(x.Parameter);
                        var nav = MopupService.Instance;
                        await nav.PopAsync(x.Animated);
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

    public static IDisposable NavigatePopAllPopupPage<TParameter>(
        this IObservable<TParameter> observable,
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
                    };
                })
            .ObserveOn(RxApp.MainThreadScheduler)
            .SelectMany(
                static async x =>
                {
                    try
                    {
                        x.PreNavigation?.Invoke(x.Parameter);
                        var nav = MopupService.Instance;
                        await nav.PopAllAsync(x.Animated);
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

    public static IDisposable NavigateRemovePopupPage<TParameter, TPage>(
        this IObservable<TParameter> observable,
        TPage page,
        Action<TPage, TParameter> preNavigation = null,
        Action<TPage, TParameter> postNavigation = null,
        bool animated = true,
        bool allowMultiple = false,
        TimeSpan? multiTapThrottleDuration = null)
        where TPage : PopupPage
    {
        return observable
            .ThrottleFirst(multiTapThrottleDuration ?? DefaultMultiTapThrottleDuration, Schedulers.ShortTermThreadPoolScheduler)
            .Where(_ => allowMultiple || !Navigating)
            .Do(static _ => Navigating = true)
            .Select(
                x =>
                {
                    return new NavigationOptions<TPage, TParameter>
                    {
                        Page = page,
                        Parameter = x,
                        PreNavigation = preNavigation,
                        PostNavigation = postNavigation,
                        Animated = animated,
                    };
                })
            .ObserveOn(RxApp.MainThreadScheduler)
            .SelectMany(
                static async x =>
                {
                    try
                    {
                        x.PreNavigation?.Invoke(x.Page, x.Parameter);
                        var nav = MopupService.Instance;
                        await nav.RemovePageAsync(x.Page, x.Animated);
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
}
