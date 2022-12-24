using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Mopups.Pages;
using Mopups.Services;
using ReactiveUI;

namespace Sui;

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
            .ThrottleFirst(multiTapThrottleDuration ?? NavigationObservableExtensions.DefaultMultiTapThrottleDuration, RxApp.TaskpoolScheduler)
            .Where(_ => allowMultiple || !NavigationObservableExtensions.Navigating)
            .Do(_ => NavigationObservableExtensions.Navigating = true)
            .ObserveOn(pageCreationScheduler ?? RxApp.TaskpoolScheduler)
            .Select(
                x =>
                {
                    var page = Application.Current.GetPage<TPage>();
                    return (Page: page, Parameter: x, IsAppearingTask: page.AppearingAsync());
                })
            .ObserveOn(RxApp.MainThreadScheduler)
            .SelectMany(
                async x =>
                {
                    preNavigation?.Invoke(x.Page, x.Parameter);

                    var nav = MopupService.Instance;
                    await Task.WhenAll(
                        x.IsAppearingTask,
                        nav.PushAsync(x.Page, animated));

                    postNavigation?.Invoke(x.Page, x.Parameter);

                    return Unit.Default;
                })
            .Do(_ => NavigationObservableExtensions.Navigating = false)
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
            .ThrottleFirst(multiTapThrottleDuration ?? NavigationObservableExtensions.DefaultMultiTapThrottleDuration, RxApp.TaskpoolScheduler)
            .Where(_ => allowMultiple || !NavigationObservableExtensions.Navigating)
            .Do(_ => NavigationObservableExtensions.Navigating = true)
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

                    var nav = MopupService.Instance;
                    await Task.WhenAll(
                        x.IsAppearingTask,
                        nav.PushAsync(x.Page, animated));

                    postNavigation?.Invoke(x.Page, x.Parameter);

                    return Unit.Default;
                })
            .Do(_ => NavigationObservableExtensions.Navigating = false)
            .Subscribe();
    }

    public static IDisposable NavigatePopPopupPage<TParameter>(
        this IObservable<TParameter> observable,
        VisualElement element,
        Action<TParameter> preNavigation = null,
        Action<TParameter> postNavigation = null,
        bool animated = true,
        bool allowMultiple = false,
        TimeSpan? multiTapThrottleDuration = null)
    {
        return observable
            .ThrottleFirst(multiTapThrottleDuration ?? NavigationObservableExtensions.DefaultMultiTapThrottleDuration, RxApp.TaskpoolScheduler)
            .Where(_ => allowMultiple || !NavigationObservableExtensions.Navigating)
            .Do(_ => NavigationObservableExtensions.Navigating = true)
            .ObserveOn(RxApp.MainThreadScheduler)
            .SelectMany(
                async parameter =>
                {
                    preNavigation?.Invoke(parameter);
                    var nav = MopupService.Instance;
                    await nav.PopAsync(animated);
                    postNavigation?.Invoke(parameter);

                    return Unit.Default;
                })
            .Do(_ => NavigationObservableExtensions.Navigating = false)
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
            .ThrottleFirst(multiTapThrottleDuration ?? NavigationObservableExtensions.DefaultMultiTapThrottleDuration, RxApp.TaskpoolScheduler)
            .Where(_ => allowMultiple || !NavigationObservableExtensions.Navigating)
            .Do(_ => NavigationObservableExtensions.Navigating = true)
            .ObserveOn(RxApp.MainThreadScheduler)
            .SelectMany(
                async parameter =>
                {
                    preNavigation?.Invoke(parameter);
                    var nav = MopupService.Instance;
                    await nav.PopAllAsync(animated);
                    postNavigation?.Invoke(parameter);

                    return Unit.Default;
                })
            .Do(_ => NavigationObservableExtensions.Navigating = false)
            .Subscribe();
    }

    public static IDisposable NavigateRemovePopupPage<TParameter, TPage>(
        this IObservable<TParameter> observable,
        TPage page,
        Action<TParameter> preNavigation = null,
        Action<TParameter> postNavigation = null,
        bool animated = true,
        bool allowMultiple = false,
        TimeSpan? multiTapThrottleDuration = null)
        where TPage : PopupPage
    {
        return observable
            .ThrottleFirst(multiTapThrottleDuration ?? NavigationObservableExtensions.DefaultMultiTapThrottleDuration, RxApp.TaskpoolScheduler)
            .Where(_ => allowMultiple || !NavigationObservableExtensions.Navigating)
            .Do(_ => NavigationObservableExtensions.Navigating = true)
            .ObserveOn(RxApp.MainThreadScheduler)
            .SelectMany(
                async parameter =>
                {
                    preNavigation?.Invoke(parameter);
                    var nav = MopupService.Instance;
                    await nav.RemovePageAsync(page, animated);
                    postNavigation?.Invoke(parameter);

                    return Unit.Default;
                })
            .Do(_ => NavigationObservableExtensions.Navigating = false)
            .Subscribe();
    }
}