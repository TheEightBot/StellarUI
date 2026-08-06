using Microsoft.UI.Dispatching;

namespace Stellar.Uno;

/// <summary>
/// An <see cref="IScheduler"/> that schedules work onto an Uno Platform UI thread via its
/// <see cref="DispatcherQueue"/>. There is no ReactiveUI 24-compatible Uno platform
/// package, so Stellar provides this itself. Work scheduled with no due time from the UI
/// thread runs inline.
/// </summary>
public sealed class UnoScheduler : IScheduler
{
    private readonly DispatcherQueue _dispatcherQueue;

    public UnoScheduler(DispatcherQueue dispatcherQueue)
    {
        ArgumentNullException.ThrowIfNull(dispatcherQueue);

        _dispatcherQueue = dispatcherQueue;
    }

    public DateTimeOffset Now => DateTimeOffset.Now;

    public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
    {
        var innerDisp = new SingleAssignmentDisposable();

        if (_dispatcherQueue.HasThreadAccess)
        {
            innerDisp.Disposable = action(this, state);

            return innerDisp;
        }

        _dispatcherQueue.TryEnqueue(
            () =>
            {
                if (!innerDisp.IsDisposed)
                {
                    innerDisp.Disposable = action(this, state);
                }
            });

        return innerDisp;
    }

    public IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action)
    {
        if (dueTime <= TimeSpan.Zero)
        {
            return Schedule(state, action);
        }

        var innerDisp = new SingleAssignmentDisposable();
        var timer = _dispatcherQueue.CreateTimer();
        timer.Interval = dueTime;
        timer.IsRepeating = false;
        timer.Tick += (_, _) =>
        {
            if (!innerDisp.IsDisposed)
            {
                innerDisp.Disposable = action(this, state);
            }
        };
        timer.Start();

        return new CompositeDisposable(
            Disposable.Create(timer, static t => t.Stop()),
            innerDisp);
    }

    public IDisposable Schedule<TState>(TState state, DateTimeOffset dueTime, Func<IScheduler, TState, IDisposable> action)
    {
        var relative = dueTime - Now;

        return relative <= TimeSpan.Zero
            ? Schedule(state, action)
            : Schedule(state, relative, action);
    }
}
