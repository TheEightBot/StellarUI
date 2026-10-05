using System.Collections;

namespace Stellar.Maui;

public static class ItemsViewExtensions
{
    /// <summary>
    /// Sets how many recycled item views of this list keep their bindings until they are
    /// rebound, which saves tearing down and rebuilding a row's bindings each time it is
    /// reused. This is opt-in: the default is 0, which tears every view down as it is
    /// recycled. 5 matches the number of holders an Android RecyclerView pool keeps for a
    /// view type; a list whose pool has been given a different size should be given the
    /// same number here.
    /// </summary>
    /// <remarks>
    /// A kept view does not run Bind, Initialized, Activated or Deactivated again when it
    /// is given its next row; only its view model's item changes. Opt in only for item
    /// views that take everything they show from bindings to the view model.
    /// </remarks>
    /// <typeparam name="TItemsView">The type of the list.</typeparam>
    /// <param name="itemsView">The list.</param>
    /// <param name="limit">The number of recycled item views that keep their bindings.</param>
    /// <returns>The list, for chaining.</returns>
    public static TItemsView RecycledItemViewLimit<TItemsView>(this TItemsView itemsView, int limit)
        where TItemsView : ItemsView
    {
        ArgumentNullException.ThrowIfNull(itemsView);
        ArgumentOutOfRangeException.ThrowIfNegative(limit);

        // Zero is what a list that never opted in already does, so it must not be what
        // gives the list a watcher.
        if (limit == 0)
        {
            ItemsHostWatcher.Find(itemsView)?.Limit = 0;

            return itemsView;
        }

        ItemsHostWatcher.For(itemsView).Limit = limit;

        return itemsView;
    }

    public static IDisposable BindItems<TVisual>(this ItemsView<TVisual> itemsView, IObservable<IEnumerable> listItems)
        where TVisual : BindableObject
    {
        var bindingDisposables = new CompositeDisposable();

        listItems
            .Do(
                items =>
                {
                    itemsView.Dispatcher.Dispatch(
                        () =>
                        {
                            itemsView.ItemsSource = null;
                            itemsView.ItemsSource = items;
                        });
                })
            .Subscribe()
            .DisposeWith(bindingDisposables);

        return Disposable.Create(
            () =>
            {
                if (itemsView != null)
                {
                    itemsView.ItemsSource = null;
                }

                bindingDisposables?.Dispose();
            });
    }

    public static IDisposable BindItems(this ItemsView itemsView, IObservable<IEnumerable> listItems)
    {
        var bindingDisposables = new CompositeDisposable();

        listItems
            .Do(
                items =>
                {
                    if (itemsView == null)
                    {
                        return;
                    }

                    itemsView.Dispatcher.Dispatch(
                        () =>
                        {
                            itemsView.ItemsSource = null;
                            itemsView.ItemsSource = items;
                        });
                })
            .Subscribe()
            .DisposeWith(bindingDisposables);

        return Disposable.Create(
            () =>
            {
                if (itemsView != null)
                {
                    itemsView.ItemsSource = null;
                }

                bindingDisposables?.Dispose();
            });
    }
}
