using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Mopups.Pages;
using Mopups.Services;
using ReactiveUI;
using Stellar.Exceptions;
using Stellar.Maui.Exceptions;
using static Stellar.Maui.NavigationObservableExtensions;

namespace Stellar.Maui;

public static class PopupNavigationObservableExtensions
{
    public static IDisposable NavigateToPopupPage<TPage>(
        this IObservable<Unit> observable,
        Action<TPage, Unit>? preNavigation = null,
        Action<TPage, Unit>? postNavigation = null,
        Action<Unit, IDictionary<string, object>>? queryParameters = null,
        bool animated = true,
        bool allowMultiple = false,
        IScheduler? pageCreationScheduler = null,
        TimeSpan? multiTapThrottleDuration = null)
        where TPage : PopupPage
    {
        return NavigateToPopupPage<Unit, TPage>(observable, preNavigation, postNavigation, queryParameters, animated, allowMultiple, pageCreationScheduler, multiTapThrottleDuration);
    }

    public static IDisposable NavigateToPopupPage<TParameter, TPage>(
        this IObservable<TParameter?> observable,
        Action<TPage, TParameter?>? preNavigation = null,
        Action<TPage, TParameter?>? postNavigation = null,
        Action<TParameter?, IDictionary<string, object>>? queryParameters = null,
        bool animated = true,
        bool allowMultiple = false,
        IScheduler? pageCreationScheduler = null,
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
                    var cplat = IPlatformApplication.Current;

                    if (cplat is null)
                    {
                        throw new PlatformNotRegisteredException();
                    }

                    var page = cplat.Services.GetRequiredService<TPage>().ThrowIfNull();

                    return new NavigationOptions<TPage, TParameter>(page, page.AppearingAsync(), page)
                    {
                        Parameter = x,
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

                        await Task.WhenAll(
                            x.IsAppearingAsync,
                            MopupService.Instance.PushAsync(x.Page, x.Animated));

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
        Func<TParameter?, TPage> pageCreator,
        Action<TPage, TParameter?>? preNavigation = null,
        Action<TPage, TParameter?>? postNavigation = null,
        Action<TParameter?, IDictionary<string, object>>? queryParameters = null,
        bool animated = true,
        bool allowMultiple = false,
        IScheduler? pageCreationScheduler = null,
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

                    return new NavigationOptions<TPage, TParameter>(page, page.AppearingAsync(), page)
                    {
                        Parameter = x,
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

                        await Task.WhenAll(
                            x.IsAppearingAsync,
                            MopupService.Instance.PushAsync(x.Page, x.Animated));

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

    public static IDisposable NavigateToPopupPage<TParameter, TPage, TViewModel>(
        this IObservable<TParameter> observable,
        Func<TParameter, TPage> pageCreator,
        Action<TPage, TParameter?>? preNavigation = null,
        Action<TPage, TParameter?>? postNavigation = null,
        Action<TParameter?, TViewModel>? viewModelMap = null,
        bool animated = true,
        bool allowMultiple = false,
        IScheduler? pageCreationScheduler = null,
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

                    return new NavigationOptions<TPage, TParameter, TViewModel>(page, page.AppearingAsync(), page)
                    {
                        Parameter = x,
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

                        await Task.WhenAll(
                            x.IsAppearingAsync,
                            MopupService.Instance.PushAsync(x.Page, x.Animated));

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

    public static IDisposable NavigatePopPopupPage<TParameter>(
        this IObservable<TParameter> observable,
        Action<TParameter?>? preNavigation = null,
        Action<TParameter?>? postNavigation = null,
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
                        await MopupService.Instance.PopAsync(x.Animated);
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
        Action<TParameter?>? preNavigation = null,
        Action<TParameter?>? postNavigation = null,
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
                        await MopupService.Instance.PopAllAsync(x.Animated);
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
        Action<TPage, TParameter?>? preNavigation = null,
        Action<TPage, TParameter?>? postNavigation = null,
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
                    return new NavigationOptions<TPage, TParameter>(page, Task.CompletedTask)
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
                        x.PreNavigation?.Invoke(x.Page, x.Parameter);
                        await MopupService.Instance.RemovePageAsync(x.Page, x.Animated);
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
