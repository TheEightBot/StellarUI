﻿using System.Reactive;
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
        bool animated = true,
        bool allowMultiple = false,
        IScheduler pageCreationScheduler = null,
        TimeSpan? multiTapThrottleDuration = null)
        where TPage : PopupPage, IStellarView
    {
        return NavigateToPopupPage<Unit, TPage>(observable, preNavigation, postNavigation, animated, allowMultiple, pageCreationScheduler, multiTapThrottleDuration);
    }

    public static IDisposable NavigateToPopupPage<TParameter, TPage>(
        this IObservable<TParameter> observable,
        Action<TPage, TParameter> preNavigation = null,
        Action<TPage, TParameter> postNavigation = null,
        bool animated = true,
        bool allowMultiple = false,
        IScheduler pageCreationScheduler = null,
        TimeSpan? multiTapThrottleDuration = null)
        where TPage : PopupPage, IStellarView
    {
        return observable
            .ThrottleFirst(multiTapThrottleDuration ?? DefaultMultiTapThrottleDuration, Schedulers.ShortTermThreadPoolScheduler)
            .Where(_ => allowMultiple || !Navigating)
            .Do(_ => Navigating = true)
            .ObserveOn(pageCreationScheduler ?? Schedulers.ShortTermThreadPoolScheduler)
            .Select(
                x =>
                {
                    var page = Application.Current.GetPage<TPage>();
                    return new NavigationOptions<TPage, TParameter>
                    {
                        Page = page,
                        Parameter = x,
                        IsAppearingAsync = page.AppearingAsync(),
                        PreNavigation = preNavigation,
                        PostNavigation = postNavigation,
                        Animated = animated,
                    };
                })
            .ObserveOn(RxApp.MainThreadScheduler)
            .SelectMany(
                static async x =>
                {
                    x.PreNavigation?.Invoke(x.Page, x.Parameter);

                    var nav = MopupService.Instance;
                    await Task.WhenAll(
                        x.IsAppearingAsync,
                        nav.PushAsync(x.Page, x.Animated));

                    x.PostNavigation?.Invoke(x.Page, x.Parameter);

                    return Unit.Default;
                })
            .Do(_ => Navigating = false)
            .Subscribe();
    }

    public static IDisposable NavigateToPopupPage<TParameter, TPage>(
        this IObservable<TParameter> observable,
        Func<TParameter, TPage> pageCreator,
        Action<TPage, TParameter> preNavigation = null,
        Action<TPage, TParameter> postNavigation = null,
        bool animated = true,
        bool allowMultiple = false,
        IScheduler pageCreationScheduler = null,
        TimeSpan? multiTapThrottleDuration = null)
        where TPage : PopupPage, IStellarView
    {
        return observable
            .ThrottleFirst(multiTapThrottleDuration ?? DefaultMultiTapThrottleDuration, Schedulers.ShortTermThreadPoolScheduler)
            .Where(_ => allowMultiple || !Navigating)
            .Do(_ => Navigating = true)
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
                    };
                })
            .ObserveOn(RxApp.MainThreadScheduler)
            .SelectMany(
                async x =>
                {
                    preNavigation?.Invoke(x.Page, x.Parameter);

                    var nav = MopupService.Instance;
                    await Task.WhenAll(
                        x.IsAppearingAsync,
                        nav.PushAsync(x.Page, animated));

                    postNavigation?.Invoke(x.Page, x.Parameter);

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
            .Do(_ => Navigating = true)
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
                    x.PreNavigation?.Invoke(x.Parameter);
                    var nav = MopupService.Instance;
                    await nav.PopAsync(x.Animated);
                    x.PostNavigation?.Invoke(x.Parameter);

                    return Unit.Default;
                })
            .Do(_ => Navigating = false)
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
            .Do(_ => Navigating = true)
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
                    x.PreNavigation?.Invoke(x.Parameter);
                    var nav = MopupService.Instance;
                    await nav.PopAllAsync(x.Animated);
                    x.PostNavigation?.Invoke(x.Parameter);

                    return Unit.Default;
                })
            .Do(_ => Navigating = false)
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
        where TPage : PopupPage, IStellarView
    {
        return observable
            .ThrottleFirst(multiTapThrottleDuration ?? DefaultMultiTapThrottleDuration, Schedulers.ShortTermThreadPoolScheduler)
            .Where(_ => allowMultiple || !Navigating)
            .Do(_ => Navigating = true)
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
                    x.PreNavigation?.Invoke(x.Page, x.Parameter);
                    var nav = MopupService.Instance;
                    await nav.RemovePageAsync(x.Page, x.Animated);
                    x.PostNavigation?.Invoke(x.Page, x.Parameter);

                    return Unit.Default;
                })
            .Do(_ => Navigating = false)
            .Subscribe();
    }
}