namespace EightBot.Stellar.Maui;

public static class ListViewExtensions
{
    public static IObservable<T> ItemTapped<T>(this ListView listView)
        where T : class
    {
        return
            Observable
                .FromEvent<EventHandler<ItemTappedEventArgs>, ItemTappedEventArgs>(
                    eventHandler =>
                    {
                        void Handler(object sender, ItemTappedEventArgs e) => eventHandler?.Invoke(e);
                        return Handler;
                    },
                    x => listView.ItemTapped += x,
                    x => listView.ItemTapped -= x)
                .Select(x => x.Item)
                .OfType<T>();
    }

    public static IObservable<object> ItemTapped(this ListView listView)
    {
        return
            Observable
                .FromEvent<EventHandler<ItemTappedEventArgs>, ItemTappedEventArgs>(
                    eventHandler =>
                    {
                        void Handler(object sender, ItemTappedEventArgs e) => eventHandler?.Invoke(e);
                        return Handler;
                    },
                    x => listView.ItemTapped += x,
                    x => listView.ItemTapped -= x)
                .Select(args => args.Item);
    }

    public static IObservable<T> ItemSelected<T>(this ListView listView)
        where T : class
    {
        return
            Observable
                .FromEvent<EventHandler<SelectedItemChangedEventArgs>, SelectedItemChangedEventArgs>(
                    eventHandler =>
                    {
                        void Handler(object sender, SelectedItemChangedEventArgs e) => eventHandler?.Invoke(e);
                        return Handler;
                    },
                    x => listView.ItemSelected += x,
                    x => listView.ItemSelected -= x)
                .Select(x => x.SelectedItem)
                .OfType<T>();
    }

    public static IObservable<object> ListViewItemSelected(this ListView listView)
    {
        return
            Observable
                .FromEvent<EventHandler<SelectedItemChangedEventArgs>, SelectedItemChangedEventArgs>(
                    eventHandler =>
                    {
                        void Handler(object sender, SelectedItemChangedEventArgs e) => eventHandler?.Invoke(e);
                        return Handler;
                    },
                    x => listView.ItemSelected += x,
                    x => listView.ItemSelected -= x)
                .Select(args => args.SelectedItem);
    }
}