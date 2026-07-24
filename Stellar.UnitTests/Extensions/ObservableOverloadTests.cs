using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Reactive.Testing;

namespace Stellar.UnitTests.Extensions;

/// <summary>
/// The overloads of the Select* family that take a CancellationToken or an explicit
/// scheduler, plus the ThrottleFirst variant with before and after callbacks.
/// </summary>
public class ObservableOverloadTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private static readonly IScheduler Pool = TaskPoolScheduler.Default;

    private static IList<T> Drain<T>(IObservable<T> observable) =>
        observable.ToList().Timeout(Timeout).Wait();

    [Fact]
    public void IsNotNull_OnAReferenceStream_DropsNulls()
    {
        var results = Drain(new object?[] { 1, null, "two" }.ToObservable().IsNotNull());

        Assert.Equal(2, results.Count);
        Assert.All(results, Assert.NotNull);
    }

    [Fact]
    public void SelectConcurrent_ActionWithCancellationToken_RunsForEveryElement()
    {
        using var cts = new CancellationTokenSource();
        var tokens = new List<CancellationToken>();

        Drain(
            Observable.Range(1, 4)
                .SelectConcurrent(
                    (int _, CancellationToken token) =>
                    {
                        lock (tokens)
                        {
                            tokens.Add(token);
                        }
                    },
                    cts.Token));

        Assert.Equal(4, tokens.Count);
        Assert.All(tokens, t => Assert.Equal(cts.Token, t));
    }

    [Fact]
    public void SelectConcurrent_FuncWithCancellationToken_ProjectsResults()
    {
        using var cts = new CancellationTokenSource();

        var results = Drain(
            Observable.Range(1, 4).SelectConcurrent((int x, CancellationToken _) => x * 3, cts.Token));

        Assert.Equal(new[] { 3, 6, 9, 12 }, results.OrderBy(static x => x));
    }

    [Fact]
    public void SelectSequential_ActionWithCancellationToken_RunsInOrder()
    {
        using var cts = new CancellationTokenSource();
        var order = new List<int>();

        Drain(
            Observable.Range(1, 5)
                .SelectSequential(
                    (int x, CancellationToken _) =>
                    {
                        lock (order)
                        {
                            order.Add(x);
                        }
                    },
                    cts.Token));

        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, order);
    }

    [Fact]
    public void SelectManyConcurrent_WithCancellationToken_RunsForEveryElement()
    {
        var calls = 0;

        Drain(
            Observable.Range(1, 5)
                .SelectManyConcurrent(async (int _, CancellationToken _) =>
                {
                    Interlocked.Increment(ref calls);
                    await Task.Yield();
                }));

        Assert.Equal(5, calls);
    }

    [Fact]
    public void SelectManyConcurrent_ProjectingWithCancellationToken_ReturnsResults()
    {
        var results = Drain(
            Observable.Range(1, 4)
                .SelectManyConcurrent(async (int x, CancellationToken _) =>
                {
                    await Task.Yield();
                    return x + 100;
                }));

        Assert.Equal(new[] { 101, 102, 103, 104 }, results.OrderBy(static x => x));
    }

    [Fact]
    public void SelectManySequential_WithoutAParameter_RunsOncePerElement()
    {
        var calls = 0;

        Drain(
            Observable.Range(1, 5)
                .SelectManySequential(async () =>
                {
                    Interlocked.Increment(ref calls);
                    await Task.Yield();
                }));

        Assert.Equal(5, calls);
    }

    [Fact]
    public void SelectManySequential_WithCancellationToken_RunsInOrder()
    {
        var order = new List<int>();

        Drain(
            Observable.Range(1, 5)
                .SelectManySequential(async (int x, CancellationToken _) =>
                {
                    lock (order)
                    {
                        order.Add(x);
                    }

                    await Task.Yield();
                }));

        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, order);
    }

    [Fact]
    public void SelectManySequential_ProjectingWithCancellationToken_PreservesOrder()
    {
        var results = Drain(
            Observable.Range(1, 5)
                .SelectManySequential(async (int x, CancellationToken _) =>
                {
                    await Task.Yield();
                    return x * 10;
                }));

        Assert.Equal(new[] { 10, 20, 30, 40, 50 }, results);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SelectConcurrent_WithAndWithoutAScheduler_ProducesTheSameResults(bool withScheduler)
    {
        var results = Drain(
            Observable.Range(1, 4)
                .SelectConcurrent(static (int x) => x * 2, concurrentSubscriptions: 2, scheduler: withScheduler ? Pool : null));

        Assert.Equal(new[] { 2, 4, 6, 8 }, results.OrderBy(static x => x));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SelectSequential_WithAndWithoutAScheduler_PreservesOrder(bool withScheduler)
    {
        var results = Drain(
            Observable.Range(1, 4)
                .SelectSequential(static (int x) => x * 2, scheduler: withScheduler ? Pool : null));

        Assert.Equal(new[] { 2, 4, 6, 8 }, results);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SelectManyConcurrent_WithAndWithoutAScheduler_ProducesResults(bool withScheduler)
    {
        var results = Drain(
            Observable.Range(1, 4)
                .SelectManyConcurrent(
                    static async (int x) =>
                    {
                        await Task.Yield();
                        return x * 2;
                    },
                    concurrentSubscriptions: 2,
                    scheduler: withScheduler ? Pool : null));

        Assert.Equal(new[] { 2, 4, 6, 8 }, results.OrderBy(static x => x));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SelectManySequential_WithAndWithoutAScheduler_PreservesOrder(bool withScheduler)
    {
        var results = Drain(
            Observable.Range(1, 4)
                .SelectManySequential(
                    static async (int x) =>
                    {
                        await Task.Yield();
                        return x * 2;
                    },
                    scheduler: withScheduler ? Pool : null));

        Assert.Equal(new[] { 2, 4, 6, 8 }, results);
    }

    [Fact]
    public void ThrottleFirst_WithCallbacks_EmitsAndBracketsTheThrottleWindow()
    {
        var scheduler = new TestScheduler();
        var source = new Subject<int>();
        var received = new List<int>();
        var before = new List<int>();
        var after = new List<int>();

        using var subscription = source
            .ThrottleFirst(
                v => before.Add(v),
                v => after.Add(v),
                TimeSpan.FromSeconds(1),
                scheduler,
                scheduler)
            .Subscribe(v => received.Add(v));

        source.OnNext(1);

        // The value is emitted immediately; the callbacks are scheduled around the window.
        Assert.Equal(new[] { 1 }, received);

        scheduler.Start();

        Assert.Equal(new[] { 1 }, before);
        Assert.Equal(new[] { 1 }, after);
    }

    [Fact]
    public void ThrottleFirst_WithCallbacks_SuppressesValuesInsideTheWindow()
    {
        var scheduler = new TestScheduler();
        var source = new Subject<int>();
        var received = new List<int>();

        using var subscription = source
            .ThrottleFirst(
                static _ => { },
                static _ => { },
                TimeSpan.FromSeconds(1),
                scheduler,
                scheduler)
            .Subscribe(v => received.Add(v));

        source.OnNext(1);
        source.OnNext(2);
        source.OnNext(3);

        Assert.Equal(new[] { 1 }, received);
    }

    [Fact]
    public void ThrottleFirst_WithCallbacks_ReopensAfterTheWindow()
    {
        var scheduler = new TestScheduler();
        var source = new Subject<int>();
        var received = new List<int>();

        using var subscription = source
            .ThrottleFirst(
                static _ => { },
                static _ => { },
                TimeSpan.FromSeconds(1),
                scheduler,
                scheduler)
            .Subscribe(v => received.Add(v));

        source.OnNext(1);
        scheduler.AdvanceBy(TimeSpan.FromSeconds(2).Ticks);
        source.OnNext(2);

        Assert.Equal(new[] { 1, 2 }, received);
    }

    [Fact]
    public void ThrottleFirst_WithCallbacks_PropagatesErrorsAndCompletion()
    {
        var scheduler = new TestScheduler();
        var source = new Subject<int>();
        Exception? caught = null;

        using var subscription = source
            .ThrottleFirst(static _ => { }, static _ => { }, TimeSpan.FromSeconds(1), scheduler, scheduler)
            .Subscribe(static _ => { }, ex => caught = ex);

        var boom = new InvalidOperationException("boom");
        source.OnError(boom);

        Assert.Same(boom, caught);
    }
}
