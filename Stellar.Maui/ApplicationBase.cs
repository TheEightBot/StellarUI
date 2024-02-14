namespace Stellar.Maui;

public abstract class ApplicationBase : Application
{
    private readonly Lazy<Subject<ApplicationLifecycleEvent>> _lifecycle = new Lazy<Subject<ApplicationLifecycleEvent>>(() => new Subject<ApplicationLifecycleEvent>(), LazyThreadSafetyMode.ExecutionAndPublication);

    public IObservable<Unit> IsStarting => _lifecycle.Value.Where(x => x == ApplicationLifecycleEvent.IsStarting).SelectUnit().AsObservable();

    public IObservable<Unit> IsResuming => _lifecycle.Value.Where(x => x == ApplicationLifecycleEvent.IsResuming).SelectUnit().AsObservable();

    public IObservable<Unit> IsSleeping => _lifecycle.Value.Where(x => x == ApplicationLifecycleEvent.IsSleeping).SelectUnit().AsObservable();

    public IObservable<ApplicationLifecycleEvent> Lifecycle => _lifecycle.Value.AsObservable();

    protected override void OnStart()
    {
        base.OnStart();

        if (!_lifecycle.IsValueCreated)
        {
            return;
        }

        _lifecycle.Value.OnNext(ApplicationLifecycleEvent.IsStarting);
    }

    protected override void OnResume()
    {
        base.OnResume();

        if (!_lifecycle.IsValueCreated)
        {
            return;
        }

        _lifecycle.Value.OnNext(ApplicationLifecycleEvent.IsResuming);
    }

    protected override void OnSleep()
    {
        base.OnSleep();

        if (!_lifecycle.IsValueCreated)
        {
            return;
        }

        _lifecycle.Value.OnNext(ApplicationLifecycleEvent.IsSleeping);
    }
}
