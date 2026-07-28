using System.Reactive.Subjects;
using Microsoft.Reactive.Testing;

namespace Stellar.UnitTests.Extensions;

/// <summary>
/// ObserveLatestOn is a conflating observe-on: while the target scheduler is busy
/// it keeps only the most recent value and drops the ones in between. A TestScheduler
/// makes that deterministic, because nothing drains until the test advances time.
/// </summary>
public class ObserveLatestOnTests
{
    [Fact]
    public void DeliversValuesOnTheSuppliedScheduler()
    {
        var scheduler = new TestScheduler();
        var source = new Subject<int>();
        var received = new List<int>();

        using var subscription = source.ObserveLatestOn(scheduler).Subscribe(received.Add);

        source.OnNext(1);

        // Nothing is delivered until the scheduler runs.
        Assert.Empty(received);

        scheduler.Start();

        Assert.Equal(new[] { 1 }, received);
    }

    [Fact]
    public void CoalescesToTheLatestValueWhileTheSchedulerIsIdle()
    {
        var scheduler = new TestScheduler();
        var source = new Subject<int>();
        var received = new List<int>();

        using var subscription = source.ObserveLatestOn(scheduler).Subscribe(received.Add);

        source.OnNext(1);
        source.OnNext(2);
        source.OnNext(3);

        scheduler.Start();

        // 1 and 2 are superseded before the drain loop ever runs.
        Assert.Equal(new[] { 3 }, received);
    }

    [Fact]
    public void DeliversANullValueRatherThanDroppingIt()
    {
        // Regression guard: an earlier implementation decided whether a value was
        // pending by null-checking the stored value, which silently swallowed a
        // legitimate null. The pending flag is what makes this work.
        var scheduler = new TestScheduler();
        var source = new Subject<string?>();
        var received = new List<string?>();

        using var subscription = source.ObserveLatestOn(scheduler).Subscribe(received.Add);

        source.OnNext(null);
        scheduler.Start();

        Assert.Single(received);
        Assert.Null(received[0]);
    }

    [Fact]
    public void DeliversDefaultValueTypesRatherThanDroppingThem()
    {
        // Same regression, value-type flavour: default(int) is 0 and must not be
        // mistaken for "nothing pending".
        var scheduler = new TestScheduler();
        var source = new Subject<int>();
        var received = new List<int>();

        using var subscription = source.ObserveLatestOn(scheduler).Subscribe(received.Add);

        source.OnNext(0);
        scheduler.Start();

        Assert.Equal(new[] { 0 }, received);
    }

    [Fact]
    public void PropagatesCompletion()
    {
        var scheduler = new TestScheduler();
        var source = new Subject<int>();
        var completed = false;

        using var subscription = source.ObserveLatestOn(scheduler).Subscribe(_ => { }, () => completed = true);

        source.OnNext(1);
        source.OnCompleted();
        scheduler.Start();

        Assert.True(completed);
    }

    [Fact]
    public void PropagatesErrors()
    {
        var scheduler = new TestScheduler();
        var source = new Subject<int>();
        Exception? caught = null;

        using var subscription = source.ObserveLatestOn(scheduler).Subscribe(_ => { }, ex => caught = ex);

        var boom = new InvalidOperationException("boom");
        source.OnError(boom);
        scheduler.Start();

        Assert.Same(boom, caught);
    }

    [Fact]
    public void AnErrorSupersedesAPendingValue()
    {
        var scheduler = new TestScheduler();
        var source = new Subject<int>();
        var received = new List<int>();
        Exception? caught = null;

        using var subscription = source.ObserveLatestOn(scheduler).Subscribe(received.Add, ex => caught = ex);

        var boom = new InvalidOperationException("boom");
        source.OnNext(1);
        source.OnError(boom);
        scheduler.Start();

        Assert.Same(boom, caught);
        Assert.Empty(received);
    }

    [Fact]
    public void DeliversAValueThatArrivesAfterAnEarlierDrain()
    {
        var scheduler = new TestScheduler();
        var source = new Subject<int>();
        var received = new List<int>();

        using var subscription = source.ObserveLatestOn(scheduler).Subscribe(received.Add);

        source.OnNext(1);
        scheduler.AdvanceBy(1);

        source.OnNext(2);
        scheduler.AdvanceBy(1);

        Assert.Equal(new[] { 1, 2 }, received);
    }
}
