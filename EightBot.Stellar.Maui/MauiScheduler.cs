using Microsoft.Maui.Dispatching;

namespace EightBot.Stellar.Maui;

public class MauiScheduler : IScheduler
{
    private IDispatcher _dispatcher;

    public MauiScheduler(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public DateTimeOffset Now => DateTimeOffset.Now;

    public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
    {
        var innerDisp = new SingleAssignmentDisposable();

        _dispatcher
            .Dispatch(
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
        var innerDisp = Disposable.Empty;
        bool isCancelled = false;

        _dispatcher
            .DispatchDelayed(
                dueTime,
                () =>
                {
                    if (!isCancelled)
                    {
                        innerDisp = action(this, state);
                    }
                });

        return Disposable.Create(() =>
        {
            isCancelled = true;
            innerDisp.Dispose();
        });
    }

    public IDisposable Schedule<TState>(TState state, DateTimeOffset dueTime, Func<IScheduler, TState, IDisposable> action)
    {
        if (dueTime <= Now)
        {
            return Schedule(state, action);
        }

        return Schedule(state, dueTime - Now, action);
    }
}