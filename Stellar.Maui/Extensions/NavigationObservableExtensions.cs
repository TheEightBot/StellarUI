﻿using System.Reactive.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using ReactiveUI;
using Stellar.Exceptions;
using Stellar.ViewModel;

namespace Stellar.Maui;

public static class NavigationObservableExtensions
{
    private static ulong _navigatingCount;

    /* This is roughly 12 UI ticks at 60fps. Slightly more than 200ms */
    public static TimeSpan DefaultMultiTapThrottleDuration { get; set; }
        = TimeSpan.FromMilliseconds(17 * 12);

    public static bool Navigating
    {
        get => Interlocked.Read(ref _navigatingCount) > 0;

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
        Action<TPage, Unit>? preNavigation = null,
        Action<TPage, Unit>? postNavigation = null,
        Action<Unit, IDictionary<string, object>>? queryParameters = null,
        bool animated = true,
        bool allowMultiple = false,
        IScheduler? pageCreationScheduler = null,
        TimeSpan? multiTapThrottleDuration = null)
        where TPage : Page
    {
        return NavigateToPage<Unit, TPage>(observable, element, preNavigation, postNavigation, queryParameters, animated, allowMultiple, pageCreationScheduler, multiTapThrottleDuration);
    }

    public static IDisposable NavigateToPage<TParameter, TPage>(
        this IObservable<TParameter> observable,
        VisualElement element,
        Action<TPage, TParameter?>? preNavigation = null,
        Action<TPage, TParameter?>? postNavigation = null,
        Action<TParameter?, IDictionary<string, object>>? queryParameters = null,
        bool animated = true,
        bool allowMultiple = false,
        IScheduler? pageCreationScheduler = null,
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
                    var cplat = IPlatformApplication.Current;

                    if (cplat is null)
                    {
                        throw new PlatformNotRegisteredException();
                    }

                    var page = cplat.Services.GetRequiredService<TPage>().ThrowIfNull();

                    return new NavigationOptions<TPage, TParameter>(page, page.AppearingAsync(), element)
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

    public static IDisposable NavigateToPage<TParameter, TPage, TViewModel>(
        this IObservable<TParameter> observable,
        VisualElement element,
        Action<TPage, TParameter?>? preNavigation = null,
        Action<TPage, TParameter?>? postNavigation = null,
        Action<TParameter?, TViewModel>? viewModelMap = null,
        bool animated = true,
        bool allowMultiple = false,
        IScheduler? pageCreationScheduler = null,
        TimeSpan? multiTapThrottleDuration = null)
        where TPage : Page, IViewFor<TViewModel>
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
                    var cplat = IPlatformApplication.Current;

                    if (cplat is null)
                    {
                        throw new PlatformNotRegisteredException();
                    }

                    var page = cplat.Services.GetRequiredService<TPage>().ThrowIfNull();

                    return new NavigationOptions<TPage, TParameter, TViewModel>(page, page.AppearingAsync(), element)
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
        Func<TParameter?, TPage> pageCreator,
        Action<TPage, TParameter?>? preNavigation = null,
        Action<TPage, TParameter?>? postNavigation = null,
        Action<TParameter?, IDictionary<string, object>>? queryParameters = null,
        bool animated = true,
        bool allowMultiple = false,
        IScheduler? pageCreationScheduler = null,
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

                    return new NavigationOptions<TPage, TParameter>(page, page.AppearingAsync(), element)
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
        Action<TParameter?>? preNavigation = null,
        Action<TParameter?>? postNavigation = null,
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
                    return new NavigationOptions<TParameter>(element)
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
                        var pages = await x.NavigationRoot!.PopTo<TPage>(x.Animated);
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
                    return new NavigationOptions<TParameter>(element)
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
                    return new NavigationOptions<TParameter>(element)
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
        Action<TPage, Unit>? preNavigation = null,
        Action<TPage, Unit>? postNavigation = null,
        Action<Unit, IDictionary<string, object>>? queryParameters = null,
        bool animated = true,
        bool allowMultiple = false,
        IScheduler? pageCreationScheduler = null,
        TimeSpan? multiTapThrottleDuration = null)
        where TPage : Page
    {
        return NavigateToModalPage<Unit, TPage>(observable, element, preNavigation, postNavigation, queryParameters, animated, allowMultiple, pageCreationScheduler, multiTapThrottleDuration);
    }

    public static IDisposable NavigateToModalPage<TParameter, TPage>(
        this IObservable<TParameter> observable,
        VisualElement element,
        Action<TPage, TParameter?>? preNavigation = null,
        Action<TPage, TParameter?>? postNavigation = null,
        Action<TParameter?, IDictionary<string, object>>? queryParameters = null,
        bool animated = true,
        bool allowMultiple = false,
        IScheduler? pageCreationScheduler = null,
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
                    var cplat = IPlatformApplication.Current;

                    if (cplat is null)
                    {
                        throw new PlatformNotRegisteredException();
                    }

                    var page = cplat.Services.GetRequiredService<TPage>().ThrowIfNull();

                    return new NavigationOptions<TPage, TParameter>(page, page.AppearingAsync(), element)
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
        Func<TParameter?, TPage> pageCreator,
        Action<TPage, TParameter?>? preNavigation = null,
        Action<TPage, TParameter?>? postNavigation = null,
        Action<TParameter?, IDictionary<string, object>>? queryParameters = null,
        bool animated = true,
        bool allowMultiple = false,
        IScheduler? pageCreationScheduler = null,
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

                    return new NavigationOptions<TPage, TParameter>(page, page.AppearingAsync(), element)
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

    public static IDisposable NavigateToModalPage<TParameter, TPage, TViewModel>(
        this IObservable<TParameter> observable,
        VisualElement element,
        Func<TParameter?, TPage> pageCreator,
        Action<TPage, TParameter?>? preNavigation = null,
        Action<TPage, TParameter?>? postNavigation = null,
        Action<TParameter?, TViewModel>? viewModelMap = null,
        bool animated = true,
        bool allowMultiple = false,
        IScheduler? pageCreationScheduler = null,
        TimeSpan? multiTapThrottleDuration = null)
        where TPage : Page, IViewFor<TViewModel>
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

                    return new NavigationOptions<TPage, TParameter, TViewModel>(page, page.AppearingAsync(), element)
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
                    return new NavigationOptions<TParameter>(element)
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

    public static void SetViewModelParameters<TPage>(TPage page, IDictionary<string, object> queryParameters)
        where TPage : Page
    {
        var viewModel = page.BindingContext;

        if (viewModel is null)
        {
            return;
        }

        var type = viewModel.GetType();

        var queryProperties =
            type.GetProperties()
                .Where(prop =>
                    prop.GetCustomAttributes(typeof(QueryParameterAttribute), true).Length > 0 &&
                    prop.CanWrite &&
                    (prop.SetMethod?.IsPublic ?? false));

        foreach (var prop in queryProperties)
        {
            var matchingKey = queryParameters.FirstOrDefault(x => x.Key.Equals(prop.Name, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(matchingKey.Key))
            {
                var value = matchingKey.Value;

                if (prop.PropertyType == typeof(string))
                {
                    if (value != null)
                    {
                        value = global::System.Net.WebUtility.UrlDecode((string)value);
                    }

                    prop.SetValue(viewModel, value);
                }
                else
                {
                    var castValue = Convert.ChangeType(value, prop.PropertyType);
                    prop.SetValue(viewModel, castValue);
                }
            }
        }

        if (page is IQueryAttributable pqa)
        {
            pqa.ApplyQueryAttributes(queryParameters);
        }

        if (viewModel is IQueryAttributable vmqa)
        {
            vmqa.ApplyQueryAttributes(queryParameters);
        }
    }

    public record NavigationOptions<TPage, TParameter, TViewModel>
        : NavigationOptions<TPage, TParameter>
        where TPage : Page
    {
        public NavigationOptions(TPage page, Task isAppearingAsync, VisualElement navigationRoot)
            : base(page, isAppearingAsync, navigationRoot)
        {
        }

        public Action<TParameter?, TViewModel>? ViewModelMap { get; set; }
    }

    public record NavigationOptions<TPage, TParameter>
        : NavigationOptions<TParameter>
        where TPage : Page
    {
        public NavigationOptions(TPage page, Task isAppearingAsync, VisualElement navigationRoot)
            : base(navigationRoot)
        {
            Page = page;
            IsAppearingAsync = isAppearingAsync;
        }

        public NavigationOptions(TPage page, Task isAppearingAsync)
        {
            Page = page;
            IsAppearingAsync = isAppearingAsync;
        }

        public TPage Page { get; }

        public Task IsAppearingAsync { get; }

        public new Action<TPage, TParameter?>? PreNavigation { get; set; }

        public new Action<TPage, TParameter?>? PostNavigation { get; set; }
    }

    public record NavigationOptions<TParameter>
    {
        public NavigationOptions(VisualElement navigationRoot)
        {
            NavigationRoot = navigationRoot;
        }

        public NavigationOptions()
        {
        }

        public VisualElement? NavigationRoot { get; }

        public TParameter? Parameter { get; set; }

        public Action<TParameter?, IDictionary<string, object>>? QueryParameters { get; set; }

        public Action<TParameter?>? PreNavigation { get; set; }

        public Action<TParameter?>? PostNavigation { get; set; }

        public bool Animated { get; set; }
    }
}
