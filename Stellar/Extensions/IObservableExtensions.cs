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
        // The Select existed only to drop the null annotation. TSource? and TSource are the
        // same type at runtime, so forgiving the result removes an operator from the chain.
        return source.Where(static obj => obj is not null)!;
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
        return source.Where(static x => x.HasValue && !EqualityComparer<T>.Default.Equals(x.Value, default));
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
        return source.Where(static str => !string.IsNullOrEmpty(str))!;
    }

    public static IObservable<string> IsNotNull(this IObservable<string?> source)
    {
        return source.Where(static str => str is not null)!;
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
        return source
            .Select(AsUnitWork(onNext, scheduler))
            .Merge(concurrentSubscriptions);
    }

    public static IObservable<Unit> SelectConcurrent<T>(this IObservable<T> source, Action<T, CancellationToken> onNext, CancellationToken cancellationToken, int concurrentSubscriptions = 1, IScheduler? scheduler = null)
    {
        return source
            .Select(AsUnitWork(onNext, cancellationToken, scheduler))
            .Merge(concurrentSubscriptions);
    }

    public static IObservable<TOut> SelectConcurrent<TIn, TOut>(this IObservable<TIn> source, Func<TIn, TOut> onNext, int concurrentSubscriptions = 1, IScheduler? scheduler = null)
    {
        return source
            .Select(AsWork(onNext, scheduler))
            .Merge(concurrentSubscriptions);
    }

    public static IObservable<TOut> SelectConcurrent<TIn, TOut>(this IObservable<TIn> source, Func<TIn, CancellationToken, TOut> onNext, CancellationToken cancellationToken, int concurrentSubscriptions = 1, IScheduler? scheduler = null)
    {
        return source
            .Select(AsWork(onNext, cancellationToken, scheduler))
            .Merge(concurrentSubscriptions);
    }

    public static IObservable<Unit> SelectSequential<T>(this IObservable<T> source, Action<T> onNext, IScheduler? scheduler = null)
    {
        return source
            .Select(AsUnitWork(onNext, scheduler))
            .Concat();
    }

    public static IObservable<Unit> SelectSequential<T>(this IObservable<T> source, Action<T, CancellationToken> onNext, CancellationToken cancellationToken, IScheduler? scheduler = null)
    {
        return source
            .Select(AsUnitWork(onNext, cancellationToken, scheduler))
            .Concat();
    }

    public static IObservable<TOut> SelectSequential<TIn, TOut>(this IObservable<TIn> source, Func<TIn, TOut> onNext, IScheduler? scheduler = null)
    {
        return source
            .Select(AsWork(onNext, scheduler))
            .Concat();
    }

    public static IObservable<TOut> SelectSequential<TIn, TOut>(this IObservable<TIn> source, Func<TIn, CancellationToken, TOut> onNext, CancellationToken cancellationToken, IScheduler? scheduler = null)
    {
        return source
            .Select(AsWork(onNext, cancellationToken, scheduler))
            .Concat();
    }

    // The four factories below each allocate one closure per call to the operator, not per
    // element. Everything the per-element work needs is then carried by a single
    // ScheduledWork instance rather than the nested closures the previous
    // Defer(() => Start(() => onNext(x))) shape required.
    private static Func<TIn, IObservable<TOut>> AsWork<TIn, TOut>(Func<TIn, TOut> onNext, IScheduler? scheduler)
    {
        var schedulerOrDefault = scheduler ?? Scheduler.Default;

        return x => new ScheduledWork<TIn, TOut>(x, onNext, schedulerOrDefault);
    }

    private static Func<TIn, IObservable<TOut>> AsWork<TIn, TOut>(Func<TIn, CancellationToken, TOut> onNext, CancellationToken cancellationToken, IScheduler? scheduler)
    {
        var schedulerOrDefault = scheduler ?? Scheduler.Default;

        return x => new ScheduledWork<TIn, TOut>(x, v => onNext(v, cancellationToken), schedulerOrDefault);
    }

    private static Func<T, IObservable<Unit>> AsUnitWork<T>(Action<T> onNext, IScheduler? scheduler)
    {
        var schedulerOrDefault = scheduler ?? Scheduler.Default;

        Func<T, Unit> work = x =>
        {
            onNext(x);
            return Unit.Default;
        };

        return x => new ScheduledWork<T, Unit>(x, work, schedulerOrDefault);
    }

    private static Func<T, IObservable<Unit>> AsUnitWork<T>(Action<T, CancellationToken> onNext, CancellationToken cancellationToken, IScheduler? scheduler)
    {
        var schedulerOrDefault = scheduler ?? Scheduler.Default;

        Func<T, Unit> work = x =>
        {
            onNext(x, cancellationToken);
            return Unit.Default;
        };

        return x => new ScheduledWork<T, Unit>(x, work, schedulerOrDefault);
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
            var gate = new Lock();
            bool active = false;
            bool hasValue = false;
            T? latestValue = default;
            bool hasError = false;
            Exception? latestError = null;
            bool hasCompleted = false;
            var cancelable = new MultipleAssignmentDisposable();

            void DrainLoop(Action self)
            {
                T? value;
                Exception? error;
                bool completed;
                bool hadValue;

                lock (gate)
                {
                    hadValue = hasValue;
                    value = latestValue;
                    error = latestError;
                    completed = hasCompleted;
                    latestValue = default;
                    hasValue = false;
                    hasError = false;
                    hasCompleted = false;
                }

                if (error is not null)
                {
                    observer.OnError(error);
                    return;
                }

                if (hadValue)
                {
                    observer.OnNext(value!);
                }

                if (completed)
                {
                    observer.OnCompleted();
                    return;
                }

                bool hasPending;
                lock (gate)
                {
                    hasPending = active = hasValue || hasError || hasCompleted;
                }

                if (hasPending)
                {
                    self();
                }
            }

            var disposable = source.Subscribe(
                value =>
                {
                    bool wasNotAlreadyActive;
                    lock (gate)
                    {
                        wasNotAlreadyActive = !active;
                        active = true;
                        hasValue = true;
                        latestValue = value;
                    }

                    if (wasNotAlreadyActive)
                    {
                        cancelable.Disposable = scheduler.Schedule(DrainLoop);
                    }
                },
                error =>
                {
                    bool wasNotAlreadyActive;
                    lock (gate)
                    {
                        wasNotAlreadyActive = !active;
                        active = true;
                        hasError = true;
                        latestError = error;
                    }

                    if (wasNotAlreadyActive)
                    {
                        cancelable.Disposable = scheduler.Schedule(DrainLoop);
                    }
                },
                () =>
                {
                    bool wasNotAlreadyActive;
                    lock (gate)
                    {
                        wasNotAlreadyActive = !active;
                        active = true;
                        hasCompleted = true;
                    }

                    if (wasNotAlreadyActive)
                    {
                        cancelable.Disposable = scheduler.Schedule(DrainLoop);
                    }
                });

            return new CompositeDisposable(disposable, cancelable);
        });
    }

    public static IObservable<T?> ThrottleFirst<T>(this IObservable<T?> source, TimeSpan delay, IScheduler? scheduler = null)
    {
        IScheduler schedulerOrDefault = scheduler ?? Scheduler.Default;

        return Observable.Create<T?>(observer =>
        {
            var gate = new Lock();
            bool gateOpen = true;
            var throttleDisposable = new SerialDisposable();

            var subscription = source.Subscribe(
                value =>
                {
                    bool shouldEmit;
                    lock (gate)
                    {
                        shouldEmit = gateOpen;
                        if (gateOpen)
                        {
                            gateOpen = false;
                        }
                    }

                    if (shouldEmit)
                    {
                        observer.OnNext(value);
                        throttleDisposable.Disposable = schedulerOrDefault.Schedule(delay, () =>
                        {
                            lock (gate)
                            {
                                gateOpen = true;
                            }
                        });
                    }
                },
                observer.OnError,
                observer.OnCompleted);

            return new CompositeDisposable(subscription, throttleDisposable);
        });
    }

    public static IObservable<T?> ThrottleFirst<T>(this IObservable<T?> source, Action<T?> beforeThrottle, Action<T?> afterThrottle, TimeSpan delay, IScheduler? beforeAndAfterThrottleScheduler = null, IScheduler? scheduler = null)
    {
        IScheduler schedulerOrDefault = scheduler ?? Scheduler.Default;
        IScheduler beforeAndAfterThrottleSchedulerOrDefault = beforeAndAfterThrottleScheduler ?? Scheduler.Default;

        return Observable.Create<T?>(observer =>
        {
            var gate = new Lock();
            bool gateOpen = true;
            var throttleDisposable = new SerialDisposable();

            var subscription = source.Subscribe(
                value =>
                {
                    bool shouldEmit;
                    lock (gate)
                    {
                        shouldEmit = gateOpen;
                        if (gateOpen)
                        {
                            gateOpen = false;
                        }
                    }

                    if (shouldEmit)
                    {
                        observer.OnNext(value);
                        throttleDisposable.Disposable = beforeAndAfterThrottleSchedulerOrDefault.Schedule(() =>
                        {
                            beforeThrottle(value);
                            throttleDisposable.Disposable = schedulerOrDefault.Schedule(delay, () =>
                            {
                                beforeAndAfterThrottleSchedulerOrDefault.Schedule(() =>
                                {
                                    afterThrottle(value);
                                    lock (gate)
                                    {
                                        gateOpen = true;
                                    }
                                });
                            });
                        });
                    }
                },
                observer.OnError,
                observer.OnCompleted);

            return new CompositeDisposable(subscription, throttleDisposable);
        });
    }
}
