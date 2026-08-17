using System.Linq.Expressions;
using System.Reflection;
using Microsoft.UI.Xaml;

namespace Stellar.Uno;

/// <summary>
/// Creates change notifications for <see cref="DependencyProperty"/>-backed properties on
/// <see cref="DependencyObject"/>s (pages, controls) so that <c>WhenAnyValue</c> chains and
/// bindings rooted at a view keep emitting after the initial value. ReactiveUI's own
/// implementation of this lives in its WinUI/Uno platform packages, none of which have a
/// ReactiveUI 24-compatible Uno release, so Stellar registers this one. Without it,
/// resolution falls back to <c>POCOObservableForProperty</c>, which only ever produces the
/// current value.
/// </summary>
public sealed class DependencyObjectObservableForProperty : ICreatesObservableForProperty
{
    public int GetAffinityForObject(Type type, string propertyName) =>
        GetAffinityForObject(type, propertyName, beforeChanged: false);

    // 4 matches ReactiveUI's own DependencyObject handlers: above the POCO fallback,
    // below the IReactiveObject and INPC handlers, which serve those objects better.
    // DependencyProperty callbacks cannot observe values before they change, so
    // beforeChanged requests are declined and resolve elsewhere.
    public int GetAffinityForObject(Type type, string propertyName, bool beforeChanged)
    {
        if (beforeChanged || !typeof(DependencyObject).IsAssignableFrom(type))
        {
            return 0;
        }

        return GetDependencyProperty(type, propertyName) is null ? 0 : 4;
    }

    public IObservable<IObservedChange<object?, object?>> GetNotificationForProperty(object sender, Expression expression, string propertyName) =>
        GetNotificationForProperty(sender, expression, propertyName, beforeChanged: false, suppressWarnings: false);

    public IObservable<IObservedChange<object?, object?>> GetNotificationForProperty(object sender, Expression expression, string propertyName, bool beforeChanged) =>
        GetNotificationForProperty(sender, expression, propertyName, beforeChanged, suppressWarnings: false);

public IObservable<IObservedChange<object?, object?>> GetNotificationForProperty(object sender, Expression expression, string propertyName, bool beforeChanged, bool suppressWarnings)
{
    if (beforeChanged)
    {
        return Observable.Never<IObservedChange<object?, object?>>();
    }

    if (sender is not DependencyObject dependencyObject)
    {
        throw new ArgumentException($"Sender must be a DependencyObject, but was {sender?.GetType().FullName ?? "null"}.", nameof(sender));
    }

    var dependencyProperty = GetDependencyProperty(sender.GetType(), propertyName);

    if (dependencyProperty is null)
    {
        if (suppressWarnings)
        {
            return Observable.Never<IObservedChange<object?, object?>>();
        }

        throw new ArgumentException($"No DependencyProperty named '{propertyName}Property' was found on {sender.GetType().FullName}.", nameof(propertyName));
    }
        return Observable.Create<IObservedChange<object?, object?>>(
            observer =>
            {
                var token = dependencyObject.RegisterPropertyChangedCallback(
                    dependencyProperty,
                    (_, _) => observer.OnNext(new ObservedChange<object?, object?>(sender, expression, default)));

                return Disposable.Create(() => dependencyObject.UnregisterPropertyChangedCallback(dependencyProperty, token));
            });
    }

    private static DependencyProperty? GetDependencyProperty(Type type, string propertyName)
    {
        var memberName = propertyName + "Property";

        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.GetProperty(memberName, BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)?.GetValue(null) is DependencyProperty fromProperty)
            {
                return fromProperty;
            }

            if (current.GetField(memberName, BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)?.GetValue(null) is DependencyProperty fromField)
            {
                return fromField;
            }
        }

        return null;
    }
}
