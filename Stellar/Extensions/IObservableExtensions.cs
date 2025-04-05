namespace Stellar;

public static class IObservableExtensions
{
    public static IObservable<Unit> SelectUnit<TSource>(this IObservable<TSource> source)
    {
        return source
            .Select(static _ => Unit.Default);
    }

    public static IObservable<object?> AsObject<TSource>(this IObservable<TSource> source)
    {
        return source
            .Select(static x => x as object);
    }

    public static IObservable<TSource> IsNotNull<TSource>(this IObservable<TSource?> source)
    {
        return source.Where(static obj => obj is not null).Select(static obj => obj!);
    }

    public static IObservable<TSource?> IsNull<TSource>(this IObservable<TSource?> source)
        where TSource : class
    {
        return source.Where(static obj => obj is null);
    }

    public static IObservable<TSource> IsDefault<TSource>(this IObservable<TSource> source)
    {
        return source.Where(static obj => EqualityComparer<TSource>.Default.Equals(obj, default));
    }

    public static IObservable<TSource> IsNotDefault<TSource>(this IObservable<TSource> source)
    {
        return source.Where(static obj => !EqualityComparer<TSource>.Default.Equals(obj, default));
    }

    public static IObservable<T?> WhereHasValue<T>(this IObservable<T?> source)
        where T : struct
    {
        return source.Where(static x => x.HasValue);
    }

    public static IObservable<T?> WhereHasValueAndIsNot<T>(this IObservable<T?> source, T comparison)
        where T : struct
    {
        return source.Where(x => x.HasValue && !EqualityComparer<T>.Default.Equals(x.Value, comparison));
    }

    public static IObservable<T?> WhereHasValueAndIsNotDefault<T>(this IObservable<T?> source)
        where T : struct
    {
        return source.Where(x => x.HasValue && !EqualityComparer<T>.Default.Equals(x.Value, default));
    }

    public static IObservable<T?> WhereHasValueAndIs<T>(this IObservable<T?> source, T comparison)
        where T : struct
    {
        return source.Where(x => x.HasValue && EqualityComparer<T>.Default.Equals(x.Value, comparison));
    }

    public static IObservable<string> IsNotEmpty(this IObservable<string> source)
    {
        return source.Where(static str => !string.IsNullOrEmpty(str));
    }

    public static IObservable<string> IsNotNullOrEmpty(this IObservable<string?> source)
    {
        return source.Where(static str => !string.IsNullOrEmpty(str))
            .Select(str => str!);
    }

    public static IObservable<string> IsNotNull(this IObservable<string?> source)
    {
        return source
            .Where(static str => str is not null)
            .Select(str => str!);
    }

    public static IObservable<T> GetValueOrDefault<T>(this IObservable<T?> source, T defaultValue = default(T))
        where T : struct
    {
        return source.Select(x => x ?? defaultValue);
    }

    public static IObservable<bool> WhereIsTrue(this IObservable<bool> source)
    {
        return source.Where(static result => result);
    }

    public static IObservable<bool> WhereIsFalse(this IObservable<bool> source)
    {
        return source.Where(static result => !result);
    }

    public static IObservable<T> WhereIs<T>(this IObservable<T> source, T comparison)
    {
        return source.Where(result => EqualityComparer<T>.Default.Equals(result, comparison));
    }

    public static IObservable<T> WhereIsNot<T>(this IObservable<T> source, T comparison)
    {
        return source.Where(result => !EqualityComparer<T>.Default.Equals(result, comparison));
    }

    public static IObservable<bool> ValueIsFalse(this IObservable<bool> source)
    {
        return source.Select(static result => !result);
    }

    public static IObservable<bool> ValueIsTrue(this IObservable<bool> source)
    {
        return source.Select(static result => result);
    }

    public static IObservable<T> TakeOne<T>(this IObservable<T> source)
    {
        return source.Take(1);
    }

    public static IObservable<T> SkipOne<T>(this IObservable<T> source)
    {
        return source.Skip(1);
    }

    public static IObservable<Unit> SelectConcurrent<T>(this IObservable<T> source, Action<T> onNext, int concurrentSubscriptions = 1, IScheduler? scheduler = null)
    {
        if (scheduler is not null)
        {
            return source
                .Select(x =>
                    Observable
                        .Defer(() =>
                            Observable
                                .Start(() => onNext(x))
                                .SubscribeOn(scheduler)))
                .Merge(concurrentSubscriptions);
        }

        return source
            .Select(x =>
                Observable.Defer(() =>
                    Observable.Start(() => onNext(x))))
            .Merge(concurrentSubscriptions);
    }

    public static IObservable<Unit> SelectConcurrent<T>(this IObservable<T> source, Action<T, CancellationToken> onNext, CancellationToken cancellationToken, int concurrentSubscriptions = 1, IScheduler? scheduler = null)
    {
        if (scheduler is not null)
        {
            return source
                .Select(x =>
                    Observable
                        .Defer(() =>
                            Observable
                                .Start(() => onNext(x, cancellationToken))
                                .SubscribeOn(scheduler)))
                .Merge(concurrentSubscriptions);
        }

        return source
            .Select(x =>
                Observable.Defer(() =>
                    Observable.Start(() => onNext(x, cancellationToken))))
            .Merge(concurrentSubscriptions);
    }

    public static IObservable<TOut> SelectConcurrent<TIn, TOut>(this IObservable<TIn> source, Func<TIn, TOut> onNext, int concurrentSubscriptions = 1, IScheduler? scheduler = null)
    {
        if (scheduler is not null)
        {
            return source
                .Select(xIn =>
                    Observable
                        .Defer(() =>
                            Observable
                                .Start(() => onNext(xIn))
                                .SubscribeOn(scheduler)))
                .Merge(concurrentSubscriptions);
        }

        return source
            .Select(xIn =>
                Observable.Defer(() =>
                    Observable.Start(() => onNext(xIn))))
            .Merge(concurrentSubscriptions);
    }

    public static IObservable<TOut> SelectConcurrent<TIn, TOut>(this IObservable<TIn> source, Func<TIn, CancellationToken, TOut> onNext, CancellationToken cancellationToken, int concurrentSubscriptions = 1, IScheduler? scheduler = null)
    {
        if (scheduler is not null)
        {
            return source
                .Select(xIn =>
                    Observable
                        .Defer(() =>
                            Observable
                                .Start(() => onNext(xIn, cancellationToken))
                                .SubscribeOn(scheduler)))
                .Merge(concurrentSubscriptions);
        }

        return source
            .Select(xIn =>
                Observable.Defer(() =>
                    Observable.Start(() => onNext(xIn, cancellationToken))))
            .Merge(concurrentSubscriptions);
    }

    public static IObservable<Unit> SelectSequential<T>(this IObservable<T> source, Action<T, CancellationToken> onNext, CancellationToken cancellationToken, IScheduler? scheduler = null)
    {
        if (scheduler is not null)
        {
            return source
                .Select(x =>
                    Observable
                        .Defer(() =>
                            Observable
                                .Start(() => onNext(x, cancellationToken))
                                .SubscribeOn(scheduler)))
                .Concat();
        }

        return source
            .Select(x =>
                Observable.Defer(() =>
                    Observable.Start(() => onNext(x, cancellationToken))))
            .Concat();
    }

    public static IObservable<TOut> SelectSequential<TIn, TOut>(this IObservable<TIn> source, Func<TIn, TOut> onNext, IScheduler? scheduler = null)
    {
        if (scheduler is not null)
        {
            return source
                .Select(xIn =>
                    Observable
                        .Defer(() =>
                            Observable
                                .Start(() => onNext(xIn))
                                .SubscribeOn(scheduler)))
                .Concat();
        }

        return source
            .Select(xIn =>
                Observable.Defer(() =>
                    Observable.Start(() => onNext(xIn))))
            .Concat();
    }

    public static IObservable<TOut> SelectSequential<TIn, TOut>(this IObservable<TIn> source, Func<TIn, CancellationToken, TOut> onNext, CancellationToken cancellationToken, IScheduler? scheduler = null)
    {
        if (scheduler is not null)
        {
            return source
                .Select(xIn =>
                    Observable
                        .Defer(() =>
                            Observable
                                .Start(() => onNext(xIn, cancellationToken))
                                .SubscribeOn(scheduler)))
                .Concat();
        }

        return source
            .Select(xIn =>
                Observable.Defer(() =>
                    Observable.Start(() => onNext(xIn, cancellationToken))))
            .Concat();
    }

    public static IObservable<Unit> SelectManyConcurrent<T>(this IObservable<T> source, Func<T, Task> onNext, int concurrentSubscriptions = 1, IScheduler? scheduler = null)
    {
        if (scheduler is not null)
        {
            return source
                .Select(x =>
                    Observable
                        .FromAsync(() => onNext(x))
                        .SubscribeOn(scheduler))
                .Merge(concurrentSubscriptions);
        }

        return source
            .Select(x => Observable.FromAsync(() => onNext(x)))
            .Merge(concurrentSubscriptions);
    }

    public static IObservable<Unit> SelectManyConcurrent<T>(this IObservable<T> source, Func<Task> onNext, int concurrentSubscriptions = 1, IScheduler? scheduler = null)
    {
        if (scheduler is not null)
        {
            return source
                .Select(_ =>
                    Observable
                        .FromAsync(() => onNext())
                        .SubscribeOn(scheduler))
                .Merge(concurrentSubscriptions);
        }

        return source
            .Select(_ => Observable.FromAsync(() => onNext()))
            .Merge(concurrentSubscriptions);
    }

    public static IObservable<Unit> SelectManyConcurrent<T>(this IObservable<T> source, Func<T, CancellationToken, Task> onNext, int concurrentSubscriptions = 1, IScheduler? scheduler = null)
    {
        if (scheduler is not null)
        {
            return source
                .Select(x =>
                    Observable
                        .FromAsync((CancellationToken cancellationToken) => onNext(x, cancellationToken))
                        .SubscribeOn(scheduler))
                .Merge(concurrentSubscriptions);
        }

        return source
            .Select(x => Observable.FromAsync((CancellationToken cancellationToken) => onNext(x, cancellationToken)))
            .Merge(concurrentSubscriptions);
    }

    public static IObservable<TOut> SelectManyConcurrent<TIn, TOut>(this IObservable<TIn> source, Func<TIn, Task<TOut>> onNext, int concurrentSubscriptions = 1, IScheduler? scheduler = null)
    {
        if (scheduler is not null)
        {
            return source
                .Select(x =>
                    Observable
                        .FromAsync(() => onNext(x))
                        .SubscribeOn(scheduler))
                .Merge(concurrentSubscriptions);
        }

        return source
            .Select(x => Observable.FromAsync(() => onNext(x)))
            .Merge(concurrentSubscriptions);
    }

    public static IObservable<TOut> SelectManyConcurrent<TIn, TOut>(this IObservable<TIn> source, Func<TIn, CancellationToken, Task<TOut>> onNext, int concurrentSubscriptions = 1, IScheduler? scheduler = null)
    {
        if (scheduler is not null)
        {
            return source
                .Select(x =>
                    Observable
                        .FromAsync((CancellationToken cancellationToken) => onNext(x, cancellationToken))
                        .SubscribeOn(scheduler))
                .Merge(concurrentSubscriptions);
        }

        return source
            .Select(x => Observable.FromAsync((CancellationToken cancellationToken) => onNext(x, cancellationToken)))
            .Merge(concurrentSubscriptions);
    }

    public static IObservable<Unit> SelectManySequential<T>(this IObservable<T> source, Func<T, Task> onNext, IScheduler? scheduler = null)
    {
        if (scheduler is not null)
        {
            return source
                .Select(x =>
                    Observable
                        .FromAsync(() => onNext(x))
                        .SubscribeOn(scheduler))
                .Concat();
        }

        return source
            .Select(x => Observable.FromAsync(() => onNext(x)))
            .Concat();
    }

    public static IObservable<Unit> SelectManySequential<T>(this IObservable<T> source, Func<Task> onNext, IScheduler? scheduler = null)
    {
        if (scheduler is not null)
        {
            return source
                .Select(_ =>
                    Observable
                        .FromAsync(() => onNext())
                        .SubscribeOn(scheduler))
                .Concat();
        }

        return source
            .Select(_ => Observable.FromAsync(() => onNext()))
            .Concat();
    }

    public static IObservable<Unit> SelectManySequential<T>(this IObservable<T> source, Func<T, CancellationToken, Task> onNext, IScheduler? scheduler = null)
    {
        if (scheduler is not null)
        {
            return source
                .Select(x =>
                    Observable
                        .FromAsync((CancellationToken cancellationToken) => onNext(x, cancellationToken))
                        .SubscribeOn(scheduler))
                .Concat();
        }

        return source
            .Select(x => Observable.FromAsync((CancellationToken cancellationToken) => onNext(x, cancellationToken)))
            .Concat();
    }

    public static IObservable<TOut> SelectManySequential<TIn, TOut>(this IObservable<TIn> source, Func<TIn, Task<TOut>> onNext, IScheduler? scheduler = null)
    {
        if (scheduler is not null)
        {
            return source
                .Select(x =>
                    Observable
                        .FromAsync(() => onNext(x))
                        .SubscribeOn(scheduler))
                .Concat();
        }

        return source
            .Select(x => Observable.FromAsync(() => onNext(x)))
            .Concat();
    }

    public static IObservable<TOut> SelectManySequential<TIn, TOut>(this IObservable<TIn> source, Func<TIn, CancellationToken, Task<TOut>> onNext, IScheduler? scheduler = null)
    {
        if (scheduler is not null)
        {
            return source
                .Select(x =>
                    Observable
                        .FromAsync((CancellationToken cancellationToken) => onNext(x, cancellationToken))
                        .SubscribeOn(scheduler))
                .Concat();
        }

        return source
            .Select(x => Observable.FromAsync((CancellationToken cancellationToken) => onNext(x, cancellationToken)))
            .Concat();
    }

    public static IObservable<T> ObserveLatestOn<T>(this IObservable<T> source, IScheduler scheduler)
    {
        return Observable.Create<T>(observer =>
        {
            Notification<T>? outsideNotification = null;
            var gate = new object();
            bool active = false;
            var cancelable = new MultipleAssignmentDisposable();
            var disposable =
                source
                    .Materialize()
                    .Subscribe(
                        thisNotification =>
                        {
                            bool wasNotAlreadyActive;
                            lock (gate)
                            {
                                wasNotAlreadyActive = !active;
                                active = true;
                                outsideNotification = thisNotification;
                            }

                            if (wasNotAlreadyActive)
                            {
                                cancelable.Disposable =
                                    scheduler
                                        .Schedule(
                                            self =>
                                            {
                                                Notification<T>? localNotification = null;
                                                lock (gate)
                                                {
                                                    localNotification = outsideNotification;
                                                    outsideNotification = null;
                                                }

                                                localNotification.Accept(observer);

                                                bool hasPendingNotification = false;

                                                lock (gate)
                                                {
                                                    hasPendingNotification = active = outsideNotification is not null;
                                                }

                                                if (hasPendingNotification)
                                                {
                                                    self();
                                                }
                                            });
                            }
                        });
            return new CompositeDisposable(disposable, cancelable);
        });
    }

    public static IObservable<T?> ThrottleFirst<T>(this IObservable<T?> source, TimeSpan delay, IScheduler? scheduler = null)
    {
        IScheduler schedulerOrDefault = scheduler ?? Scheduler.Default;

        return source
            .Publish(
                o =>
                {
                    return o
                        .Take(1, schedulerOrDefault)
                        .Concat(o.IgnoreElements().TakeUntil(Observable.Return(default(T), schedulerOrDefault).Delay(delay, schedulerOrDefault)))
                        .Repeat()
                        .TakeUntil(o.IgnoreElements().Concat(Observable.Return(default(T), schedulerOrDefault)));
                });
    }

    public static IObservable<T?> ThrottleFirst<T>(this IObservable<T?> source, Action<T?> beforeThrottle, Action<T?> afterThrottle, TimeSpan delay, IScheduler? beforeAndAfterThrottleScheduler = null, IScheduler? scheduler = null)
    {
        IScheduler schedulerOrDefault = scheduler ?? Scheduler.Default;
        IScheduler beforeAndAfterThrottleSchedulerOrDefault = beforeAndAfterThrottleScheduler ?? Scheduler.Default;

        return source
            .Publish(
                o =>
                {
                    return o
                        .Take(1, schedulerOrDefault)
                        .Concat(
                            o.IgnoreElements()
                                .TakeUntil(
                                    Observable.Return(default(T), schedulerOrDefault)
                                        .ObserveOn(beforeAndAfterThrottleSchedulerOrDefault)
                                        .Do(beforeThrottle)
                                        .ObserveOn(schedulerOrDefault)
                                        .Delay(delay, schedulerOrDefault)
                                        .ObserveOn(beforeAndAfterThrottleSchedulerOrDefault)
                                        .Do(afterThrottle)
                                        .ObserveOn(schedulerOrDefault)))
                        .Repeat()
                        .TakeUntil(o.IgnoreElements().Concat(Observable.Return(default(T), schedulerOrDefault)));
                });
    }
}
