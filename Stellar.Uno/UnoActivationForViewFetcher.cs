using Microsoft.UI.Xaml;

namespace Stellar.Uno;

/// <summary>
/// Determines when Uno Platform views are activated and deactivated so that
/// <c>WhenActivated</c> works for them. There is no ReactiveUI 24-compatible Uno
/// platform package, so Stellar provides this itself. Views follow
/// <see cref="FrameworkElement.Loaded"/> / <see cref="FrameworkElement.Unloaded"/>.
/// </summary>
public sealed class UnoActivationForViewFetcher : IActivationForViewFetcher
{
    public int GetAffinityForView(Type view) =>
        typeof(FrameworkElement).IsAssignableFrom(view) ? 10 : 0;

    public IObservable<bool> GetActivationForView(IActivatableView view)
    {
        if (view is not FrameworkElement element)
        {
            return Observable.Return(false);
        }

        var loaded = Observable
            .FromEventPattern<RoutedEventHandler, RoutedEventArgs>(
                handler => element.Loaded += handler,
                handler => element.Loaded -= handler)
            .Select(static _ => true);

        var unloaded = Observable
            .FromEventPattern<RoutedEventHandler, RoutedEventArgs>(
                handler => element.Unloaded += handler,
                handler => element.Unloaded -= handler)
            .Select(static _ => false);

        return loaded.Merge(unloaded).DistinctUntilChanged();
    }
}
