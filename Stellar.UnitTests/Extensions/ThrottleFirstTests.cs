using System.Reactive.Subjects;
using Microsoft.Reactive.Testing;

namespace Stellar.UnitTests.Extensions;

/// <summary>
/// ThrottleFirst is leading-edge throttling: the first value goes straight through,
/// then a window closes for the given delay. It is the opposite of Rx's Throttle,
/// which emits the last value after quiet.
/// </summary>
public class ThrottleFirstTests
{
    private static readonly TimeSpan Delay = TimeSpan.FromSeconds(1);

    [Fact]
    public void EmitsTheFirstValueImmediately()
    {
        var scheduler = new TestScheduler();
        var source = new Subject<int>();
        var received = new List<int>();

        using var subscription = source.ThrottleFirst(Delay, scheduler).Subscribe(received.Add);

        source.OnNext(1);

        // Leading edge: no need to advance the clock at all.
        Assert.Equal(new[] { 1 }, received);
    }

    [Fact]
    public void SuppressesValuesInsideTheWindow()
    {
        var scheduler = new TestScheduler();
        var source = new Subject<int>();
        var received = new List<int>();

        using var subscription = source.ThrottleFirst(Delay, scheduler).Subscribe(received.Add);

        source.OnNext(1);
        source.OnNext(2);
        source.OnNext(3);

        Assert.Equal(new[] { 1 }, received);
    }

    [Fact]
    public void EmitsAgainOnceTheWindowElapses()
    {
        var scheduler = new TestScheduler();
        var source = new Subject<int>();
        var received = new List<int>();

        using var subscription = source.ThrottleFirst(Delay, scheduler).Subscribe(received.Add);

        source.OnNext(1);
        source.OnNext(2);

        scheduler.AdvanceBy(Delay.Ticks);

        source.OnNext(3);

        Assert.Equal(new[] { 1, 3 }, received);
    }

    [Fact]
    public void SuppressedValuesAreDroppedRatherThanQueued()
    {
        var scheduler = new TestScheduler();
        var source = new Subject<int>();
        var received = new List<int>();

        using var subscription = source.ThrottleFirst(Delay, scheduler).Subscribe(received.Add);

        source.OnNext(1);
        source.OnNext(2);

        // Opening the window must not flush anything that arrived while it was shut.
        scheduler.AdvanceBy(Delay.Ticks);

        Assert.Equal(new[] { 1 }, received);
    }

    [Fact]
    public void TheWindowIsStillClosedJustBeforeTheDelayElapses()
    {
        var scheduler = new TestScheduler();
        var source = new Subject<int>();
        var received = new List<int>();

        using var subscription = source.ThrottleFirst(Delay, scheduler).Subscribe(received.Add);

        source.OnNext(1);
        scheduler.AdvanceBy(Delay.Ticks - 1);
        source.OnNext(2);

        Assert.Equal(new[] { 1 }, received);
    }

    [Fact]
    public void PropagatesCompletion()
    {
        var scheduler = new TestScheduler();
        var source = new Subject<int>();
        var completed = false;

        using var subscription = source.ThrottleFirst(Delay, scheduler).Subscribe(_ => { }, () => completed = true);

        source.OnNext(1);
        source.OnCompleted();

        Assert.True(completed);
    }

    [Fact]
    public void PropagatesErrors()
    {
        var scheduler = new TestScheduler();
        var source = new Subject<int>();
        Exception? caught = null;

        using var subscription = source.ThrottleFirst(Delay, scheduler).Subscribe(_ => { }, ex => caught = ex);

        var boom = new InvalidOperationException("boom");
        source.OnError(boom);

        Assert.Same(boom, caught);
    }

    [Fact]
    public void StopsEmittingOnceUnsubscribed()
    {
        var scheduler = new TestScheduler();
        var source = new Subject<int>();
        var received = new List<int>();

        var subscription = source.ThrottleFirst(Delay, scheduler).Subscribe(received.Add);
        source.OnNext(1);
        subscription.Dispose();

        scheduler.AdvanceBy(Delay.Ticks);
        source.OnNext(2);

        Assert.Equal(new[] { 1 }, received);
    }

    [Fact]
    public void EmitsNullValues()
    {
        var scheduler = new TestScheduler();
        var source = new Subject<string?>();
        var received = new List<string?>();

        using var subscription = source.ThrottleFirst(Delay, scheduler).Subscribe(received.Add);

        source.OnNext(null);

        Assert.Single(received);
        Assert.Null(received[0]);
    }
}
