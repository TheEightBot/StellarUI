using System.Reactive;
using System.Reactive.Linq;

namespace Stellar.UnitTests.Extensions;

/// <summary>
/// The SelectConcurrent / SelectSequential / SelectMany* family runs user work off the
/// source stream, either with bounded parallelism (Merge) or strictly one at a time
/// (Concat).
/// </summary>
/// <remarks>
/// These cannot be driven with a TestScheduler: Observable.Start and Observable.FromAsync
/// dispatch onto the thread pool regardless of the scheduler argument, which only moves
/// where subscription happens. So the assertions here are about the observable contract
/// rather than about timing -- how many callbacks are in flight at once, and what order
/// they run and complete in. ConcurrencyProbe measures the first; a recorded call log
/// gives the second.
/// </remarks>
public class SelectConcurrencyTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private static IList<T> Drain<T>(IObservable<T> observable) =>
        observable.ToList().Timeout(Timeout).Wait();

    [Fact]
    public void SelectSequential_RunsOneAtATime()
    {
        using var probe = new ConcurrencyProbe();

        Drain(Observable.Range(1, 8).SelectSequential(probe.RunAndProject));

        Assert.Equal(1, probe.MaxObserved);
        Assert.Equal(8, probe.TotalCalls);
    }

    [Fact]
    public void SelectSequential_PreservesSourceOrder()
    {
        var order = new List<int>();

        var results = Drain(
            Observable.Range(1, 5)
                .SelectSequential(x =>
                {
                    lock (order)
                    {
                        order.Add(x);
                    }

                    return x * 10;
                }));

        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, order);
        Assert.Equal(new[] { 10, 20, 30, 40, 50 }, results);
    }

    [Fact]
    public void SelectConcurrent_DefaultsToOneAtATime()
    {
        // concurrentSubscriptions defaults to 1, so the default behaviour matches
        // SelectSequential in concurrency even though it composes with Merge.
        using var probe = new ConcurrencyProbe();

        Drain(Observable.Range(1, 8).SelectConcurrent(probe.Run));

        Assert.Equal(1, probe.MaxObserved);
    }

    [Fact]
    public void SelectConcurrent_NeverExceedsTheRequestedLimit()
    {
        using var probe = new ConcurrencyProbe();

        Drain(Observable.Range(1, 24).SelectConcurrent(probe.Run, concurrentSubscriptions: 3));

        Assert.True(
            probe.MaxObserved <= 3,
            $"expected at most 3 concurrent callbacks, saw {probe.MaxObserved}");
        Assert.Equal(24, probe.TotalCalls);
    }

    [Fact]
    public void SelectConcurrent_ActuallyRunsWorkInParallel()
    {
        // Every callback waits for four of them to arrive before any may return. That can
        // only complete if the operator really does run four at once, so this fails by
        // timing out rather than by asserting on a sampled maximum.
        const int Parallelism = 4;
        using var allArrived = new CountdownEvent(Parallelism);

        var completed = Observable
            .Range(1, Parallelism)
            .SelectConcurrent(
                _ =>
                {
                    allArrived.Signal();
                    Assert.True(
                        allArrived.Wait(Timeout),
                        "callbacks did not run concurrently; the operator serialised them");
                },
                concurrentSubscriptions: Parallelism)
            .ToList()
            .Timeout(Timeout)
            .Wait();

        Assert.Equal(Parallelism, completed.Count);
    }

    [Fact]
    public void SelectConcurrent_ProjectsResults()
    {
        var results = Drain(Observable.Range(1, 4).SelectConcurrent(x => x * 2));

        Assert.Equal(new[] { 2, 4, 6, 8 }, results.OrderBy(static x => x));
    }

    [Fact]
    public void SelectSequential_PropagatesErrors()
    {
        var faulted = Observable
            .Range(1, 3)
            .SelectSequential<int, int>(static x => x == 2 ? throw new InvalidOperationException("boom") : x);

        var error = Assert.Throws<InvalidOperationException>(() => Drain(faulted));
        Assert.Equal("boom", error.Message);
    }

    [Fact]
    public void SelectConcurrent_PropagatesErrors()
    {
        var faulted = Observable
            .Range(1, 3)
            .SelectConcurrent<int, int>(static _ => throw new InvalidOperationException("boom"));

        Assert.Throws<InvalidOperationException>(() => Drain(faulted));
    }

    [Fact]
    public void SelectManySequential_RunsAsyncWorkOneAtATime()
    {
        using var probe = new ConcurrencyProbe();

        Drain(Observable.Range(1, 8).SelectManySequential(probe.RunAsync));

        Assert.Equal(1, probe.MaxObserved);
        Assert.Equal(8, probe.TotalCalls);
    }

    [Fact]
    public void SelectManySequential_PreservesSourceOrder()
    {
        var results = Drain(
            Observable.Range(1, 5)
                .SelectManySequential(static async x =>
                {
                    await Task.Yield();
                    return x * 10;
                }));

        Assert.Equal(new[] { 10, 20, 30, 40, 50 }, results);
    }

    [Fact]
    public void SelectManyConcurrent_NeverExceedsTheRequestedLimit()
    {
        using var probe = new ConcurrencyProbe();

        Drain(Observable.Range(1, 24).SelectManyConcurrent(probe.RunAsync, concurrentSubscriptions: 3));

        Assert.True(
            probe.MaxObserved <= 3,
            $"expected at most 3 concurrent callbacks, saw {probe.MaxObserved}");
    }

    [Fact]
    public void SelectManyConcurrent_ProjectsResults()
    {
        var results = Drain(
            Observable.Range(1, 4)
                .SelectManyConcurrent(static async x =>
                {
                    await Task.Yield();
                    return x * 2;
                }));

        Assert.Equal(new[] { 2, 4, 6, 8 }, results.OrderBy(static x => x));
    }

    [Fact]
    public void SelectManyConcurrent_WithoutAParameter_StillRunsPerElement()
    {
        var calls = 0;

        Drain(
            Observable.Range(1, 5)
                .SelectManyConcurrent(async () =>
                {
                    Interlocked.Increment(ref calls);
                    await Task.Yield();
                }));

        Assert.Equal(5, calls);
    }

    [Fact]
    public void CancellationTokenOverloads_ReceiveTheSuppliedToken()
    {
        using var cts = new CancellationTokenSource();
        var seen = new List<bool>();

        Drain(
            Observable.Range(1, 3)
                .SelectSequential(
                    (int x, CancellationToken token) =>
                    {
                        lock (seen)
                        {
                            seen.Add(token == cts.Token);
                        }

                        return x;
                    },
                    cts.Token));

        Assert.Equal(3, seen.Count);
        Assert.All(seen, Assert.True);
    }

    [Fact]
    public void SelectSequential_OnAnEmptySource_CompletesWithoutRunningAnything()
    {
        using var probe = new ConcurrencyProbe();

        var results = Drain(Observable.Empty<int>().SelectSequential(probe.RunAndProject));

        Assert.Empty(results);
        Assert.Equal(0, probe.TotalCalls);
    }

    /// <summary>
    /// Counts how many callbacks are inside the operator at once and remembers the peak.
    /// Each callback lingers briefly so overlapping work actually overlaps in wall time.
    /// </summary>
    private sealed class ConcurrencyProbe : IDisposable
    {
        private static readonly TimeSpan Dwell = TimeSpan.FromMilliseconds(25);

        private int _current;
        private int _max;
        private int _total;

        public int MaxObserved => Volatile.Read(ref _max);

        public int TotalCalls => Volatile.Read(ref _total);

        public int RunAndProject(int value)
        {
            Run(value);
            return value;
        }

        public void Run(int value)
        {
            Enter();

            try
            {
                Thread.Sleep(Dwell);
            }
            finally
            {
                Interlocked.Decrement(ref _current);
            }
        }

        public async Task RunAsync(int value)
        {
            Enter();

            try
            {
                await Task.Delay(Dwell).ConfigureAwait(false);
            }
            finally
            {
                Interlocked.Decrement(ref _current);
            }
        }

        public void Dispose()
        {
        }

        private void Enter()
        {
            Interlocked.Increment(ref _total);
            var running = Interlocked.Increment(ref _current);

            // Raise the recorded peak if this callback pushed it higher.
            int observed;
            while (running > (observed = Volatile.Read(ref _max)))
            {
                Interlocked.CompareExchange(ref _max, running, observed);
            }
        }
    }
}
