namespace EightBot.Stellar.Maui;

public abstract class ApplicationBase : Application, IDisposable
{
    private readonly Subject<ApplicationLifecycleEvent> _lifecycle = new Subject<ApplicationLifecycleEvent>();
    private bool _disposedValue;

    public IObservable<Unit> IsStarting => _lifecycle.Where(x => x == ApplicationLifecycleEvent.IsStarting).SelectUnit().AsObservable();

    public IObservable<Unit> IsResuming => _lifecycle.Where(x => x == ApplicationLifecycleEvent.IsResuming).SelectUnit().AsObservable();

    public IObservable<Unit> IsSleeping => _lifecycle.Where(x => x == ApplicationLifecycleEvent.IsSleeping).SelectUnit().AsObservable();

    public IObservable<ApplicationLifecycleEvent> Lifecycle => _lifecycle.AsObservable();

    protected override void OnStart()
    {
        base.OnStart();

        _lifecycle.OnNext(ApplicationLifecycleEvent.IsStarting);
    }

    protected override void OnResume()
    {
        base.OnResume();

        _lifecycle.OnNext(ApplicationLifecycleEvent.IsResuming);
    }

    protected override void OnSleep()
    {
        base.OnSleep();

        _lifecycle.OnNext(ApplicationLifecycleEvent.IsSleeping);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _lifecycle?.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}