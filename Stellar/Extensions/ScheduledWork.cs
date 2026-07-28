namespace Stellar;

/// <summary>
/// An observable that runs a projection over one value on a scheduler when it is
/// subscribed to, then emits the result and completes.
/// </summary>
/// <typeparam name="TIn">The type of the value the work runs over.</typeparam>
/// <typeparam name="TOut">The type the work produces.</typeparam>
/// <remarks>
/// <para>
/// This exists for the SelectConcurrent and SelectSequential family, which produce one of
/// these per element. It replaces
/// <c>Observable.Defer(() =&gt; Observable.Start(() =&gt; work(value)))</c>, which needed two
/// nested closures per element -- one capturing the value, one capturing the work -- plus
/// the Defer and Start observables themselves. Carrying the state on a single instance
/// removes all of that.
/// </para>
/// <para>
/// Deferring matters beyond allocation. Observable.Start is hot: it begins work when it is
/// created rather than when it is subscribed to, so the Merge and Concat downstream could
/// only order results, never bound the work. Subscription is what starts the work here,
/// which is what gives Concat one-at-a-time behaviour and Merge its concurrency cap.
/// </para>
/// </remarks>
internal sealed class ScheduledWork<TIn, TOut> : IObservable<TOut>
{
    private readonly TIn _value;
    private readonly Func<TIn, TOut> _work;
    private readonly IScheduler _scheduler;

    public ScheduledWork(TIn value, Func<TIn, TOut> work, IScheduler scheduler)
    {
        _value = value;
        _work = work;
        _scheduler = scheduler;
    }

    public IDisposable Subscribe(IObserver<TOut> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        // The stateful Schedule overload carries the pair through explicitly, so the
        // callback stays static and nothing is captured here either.
        return _scheduler.Schedule(
            (Work: this, Observer: observer),
            static (_, state) =>
            {
                try
                {
                    var result = state.Work._work(state.Work._value);
                    state.Observer.OnNext(result);
                    state.Observer.OnCompleted();
                }
                catch (Exception exception)
                {
                    state.Observer.OnError(exception);
                }

                return Disposable.Empty;
            });
    }
}
