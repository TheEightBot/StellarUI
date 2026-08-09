using System.Linq.Expressions;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using ReactiveUI;

namespace Stellar.Avalonia;

/// <summary>
/// Creates change notifications for <see cref="AvaloniaProperty"/>-backed properties on
/// <see cref="AvaloniaObject"/>s (windows, controls) so that <c>WhenAnyValue</c> chains and
/// bindings rooted at a view keep emitting after the initial value. Replaces
/// Avalonia.ReactiveUI's AvaloniaObjectObservableForProperty, which has no ReactiveUI
/// 24-compatible release. Without it, resolution falls back to
/// <c>POCOObservableForProperty</c> (AvaloniaObject does not implement
/// INotifyPropertyChanged), which only ever produces the current value.
/// </summary>
public sealed class AvaloniaObjectObservableForProperty : ICreatesObservableForProperty
{
    public int GetAffinityForObject(Type type, string propertyName) =>
        GetAffinityForObject(type, propertyName, beforeChanged: false);

    // 4 matches ReactiveUI's own platform property handlers: above the POCO fallback,
    // below the IReactiveObject and INPC handlers. Only claims properties actually
    // registered with Avalonia's property system; plain CLR properties resolve elsewhere.
    // The PropertyChanged event only reports values after they change, so beforeChanged
    // requests are declined and resolve elsewhere.
    public int GetAffinityForObject(Type type, string propertyName, bool beforeChanged) =>
        !beforeChanged &&
        typeof(AvaloniaObject).IsAssignableFrom(type) &&
        AvaloniaPropertyRegistry.Instance.FindRegistered(type, propertyName) is not null
            ? 4
            : 0;

    public IObservable<IObservedChange<object?, object?>> GetNotificationForProperty(object sender, Expression expression, string propertyName) =>
        GetNotificationForProperty(sender, expression, propertyName, beforeChanged: false, suppressWarnings: false);

    public IObservable<IObservedChange<object?, object?>> GetNotificationForProperty(object sender, Expression expression, string propertyName, bool beforeChanged) =>
        GetNotificationForProperty(sender, expression, propertyName, beforeChanged, suppressWarnings: false);

    public IObservable<IObservedChange<object?, object?>> GetNotificationForProperty(object sender, Expression expression, string propertyName, bool beforeChanged, bool suppressWarnings)
    {
        if (sender is not AvaloniaObject avaloniaObject)
        {
            throw new ArgumentException($"Sender must be an AvaloniaObject, but was {sender?.GetType().FullName ?? "null"}.", nameof(sender));
        }

        var property = AvaloniaPropertyRegistry.Instance.FindRegistered(sender.GetType(), propertyName)
            ?? throw new ArgumentException($"No AvaloniaProperty named '{propertyName}' is registered on {sender.GetType().FullName}.", nameof(propertyName));

        return Observable.Create<IObservedChange<object?, object?>>(
            observer =>
            {
void Handler(object? _, AvaloniaPropertyChangedEventArgs args)
                    if (args.Property == property)
                    {
                        observer.OnNext(new ObservedChange<object?, object?>(sender, expression, default));
                    }
                }

                avaloniaObject.PropertyChanged += Handler;

                return Disposable.Create(() => avaloniaObject.PropertyChanged -= Handler);
            });
    }
}
