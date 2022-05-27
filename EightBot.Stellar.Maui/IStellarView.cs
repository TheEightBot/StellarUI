namespace EightBot.Stellar.Maui;

public interface IStellarView<TViewModel> : IViewFor<TViewModel>, IStellarView, IDisposable
    where TViewModel : class
{
}

public interface IStellarView : IDisposable
{
    IObservable<Unit> Activated { get; }

    IObservable<Unit> Deactivated { get; }

    IObservable<LifecycleEvent> Lifecycle { get; }

    CompositeDisposable ControlBindings { get; }

    bool ControlsBound { get; }

    bool MaintainBindings { get; set; }

    public abstract void SetupUserInterface();

    public abstract void BindControls();
}