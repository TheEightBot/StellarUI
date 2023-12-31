namespace Stellar;

public interface ILifecycleEventAware
{
    public void OnLifecycleEvent(LifecycleEvent lifecycleEvent);
}
