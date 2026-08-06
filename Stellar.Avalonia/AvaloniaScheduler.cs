using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using Avalonia.Threading;

namespace Stellar.Avalonia;

/// <summary>
/// An <see cref="IScheduler"/> that schedules work onto the Avalonia UI thread.
/// Replaces Avalonia.ReactiveUI's AvaloniaScheduler, which has no release compatible
/// with ReactiveUI 24. Work scheduled with no due time from the UI thread runs inline.
/// </summary>
public sealed class AvaloniaScheduler : IScheduler
{
    private AvaloniaScheduler()
    {
    }

    public static AvaloniaScheduler Instance { get; } = new();

    public DateTimeOffset Now => DateTimeOffset.Now;

    public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
    {
        var innerDisp = new SingleAssignmentDisposable();

        if (Dispatcher.UIThread.CheckAccess())
        {
            innerDisp.Disposable = action(this, state);

            return innerDisp;
        }

        Dispatcher.UIThread.Post(
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

        var timer = DispatcherTimer.RunOnce(
            () =>
            {
                if (!innerDisp.IsDisposed)
                {
                    innerDisp.Disposable = action(this, state);
                }
            },
            dueTime);

        return new CompositeDisposable(timer, innerDisp);
    }

    public IDisposable Schedule<TState>(TState state, DateTimeOffset dueTime, Func<IScheduler, TState, IDisposable> action)
    {
        var relative = dueTime - Now;

        return relative <= TimeSpan.Zero
            ? Schedule(state, action)
            : Schedule(state, relative, action);
    }
}
