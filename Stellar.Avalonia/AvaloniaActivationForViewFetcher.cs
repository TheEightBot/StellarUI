using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using ReactiveUI;

namespace Stellar.Avalonia;

/// <summary>
/// Determines when Avalonia views are activated and deactivated so that
/// <c>WhenActivated</c> works for them. Replaces Avalonia.ReactiveUI's
/// AvaloniaActivationForViewFetcher, which has no release compatible with
/// ReactiveUI 24. Windows activate on <see cref="Window.Opened"/> and deactivate on
/// <see cref="Window.Closed"/>; other controls follow visual-tree attachment.
/// </summary>
public sealed class AvaloniaActivationForViewFetcher : IActivationForViewFetcher
{
    public int GetAffinityForView(Type view) =>
        typeof(Visual).IsAssignableFrom(view) ? 10 : 0;

    public IObservable<bool> GetActivationForView(IActivatableView view)
    {
        if (view is Window window)
        {
            var opened = Observable
                .FromEventPattern<EventHandler, EventArgs>(
                    handler => window.Opened += handler,
                    handler => window.Opened -= handler)
                .Select(static _ => true);

            var closed = Observable
                .FromEventPattern<EventHandler, EventArgs>(
                    handler => window.Closed += handler,
                    handler => window.Closed -= handler)
                .Select(static _ => false);

            return opened.Merge(closed).DistinctUntilChanged();
        }

        if (view is Control control)
        {
            var attached = Observable
                .FromEventPattern<EventHandler<VisualTreeAttachmentEventArgs>, VisualTreeAttachmentEventArgs>(
                    handler => control.AttachedToVisualTree += handler,
                    handler => control.AttachedToVisualTree -= handler)
                .Select(static _ => true);

            var detached = Observable
                .FromEventPattern<EventHandler<VisualTreeAttachmentEventArgs>, VisualTreeAttachmentEventArgs>(
                    handler => control.DetachedFromVisualTree += handler,
                    handler => control.DetachedFromVisualTree -= handler)
                .Select(static _ => false);

            return attached.Merge(detached).DistinctUntilChanged();
        }

        return Observable.Return(false);
    }
}
