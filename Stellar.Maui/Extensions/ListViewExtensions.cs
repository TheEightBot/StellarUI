// StellarUI continues to support ListView for as long as .NET MAUI ships it. MAUI marks
// ListView and its cell types obsolete in favour of CollectionView, but removing this
// surface would break every consumer still using it, so the deprecation is suppressed here
// rather than propagated. Revisit when MAUI actually removes the types.
#pragma warning disable CS0618 // Type or member is obsolete

using System.Reactive.Linq;

namespace Stellar.Maui;

public static class ListViewExtensions
{
    public static IObservable<T> ItemTapped<T>(this ListView listView, bool deselect = false)
        where T : class
    {
        var observable =
            Observable
                .FromEvent<EventHandler<ItemTappedEventArgs>, ItemTappedEventArgs>(
                    static eventHandler =>
                    {
                        void Handler(object? sender, ItemTappedEventArgs e) => eventHandler?.Invoke(e);
                        return Handler;
                    },
                    x => listView.ItemTapped += x,
                    x => listView.ItemTapped -= x)
                .Select(static x => x.Item)
                .OfType<T>();

        if (deselect)
        {
            observable =
                observable
                    .Do(_ => listView.Dispatcher.Dispatch(() => listView.SelectedItem = null));
        }

        return observable;
    }

    public static IObservable<object> ItemTapped(this ListView listView, bool deselect = false)
    {
        var observable =
            Observable
                .FromEvent<EventHandler<ItemTappedEventArgs>, ItemTappedEventArgs>(
                    static eventHandler =>
                    {
                        void Handler(object? sender, ItemTappedEventArgs e) => eventHandler?.Invoke(e);
                        return Handler;
                    },
                    x => listView.ItemTapped += x,
                    x => listView.ItemTapped -= x)
                .Select(static args => args.Item);

        if (deselect)
        {
            observable =
                observable
                    .Do(_ => listView.Dispatcher.Dispatch(() => listView.SelectedItem = null));
        }

        return observable;
    }

    public static IObservable<T> ItemSelected<T>(this ListView listView, bool deselect = false)
        where T : class
    {
        var observable =
            Observable
                .FromEvent<EventHandler<SelectedItemChangedEventArgs>, SelectedItemChangedEventArgs>(
                    static eventHandler =>
                    {
                        void Handler(object? sender, SelectedItemChangedEventArgs e) => eventHandler?.Invoke(e);
                        return Handler;
                    },
                    x => listView.ItemSelected += x,
                    x => listView.ItemSelected -= x)
                .Select(static x => x.SelectedItem)
                .OfType<T>();

        if (deselect)
        {
            observable =
                observable
                    .Do(_ => listView.Dispatcher.Dispatch(() => listView.SelectedItem = null));
        }

        return observable;
    }

    public static IObservable<object> ItemSelected(this ListView listView, bool deselect = false)
    {
        var observable =
            Observable
                .FromEvent<EventHandler<SelectedItemChangedEventArgs>, SelectedItemChangedEventArgs>(
                    static eventHandler =>
                    {
                        void Handler(object? sender, SelectedItemChangedEventArgs e) => eventHandler?.Invoke(e);
                        return Handler;
                    },
                    x => listView.ItemSelected += x,
                    x => listView.ItemSelected -= x)
                .Select(static args => args.SelectedItem);

        if (deselect)
        {
            observable =
                observable
                    .Do(_ => listView.Dispatcher.Dispatch(() => listView.SelectedItem = null));
        }

        return observable;
    }
}

#pragma warning restore CS0618
