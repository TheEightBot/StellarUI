using System.Collections;

namespace Stellar.Maui;

public static class ItemsViewExtensions
{
    /// <summary>
    /// Sets how many recycled item views of this list keep their bindings until they are
    /// rebound. The default is 5, the number of holders an Android RecyclerView pool keeps
    /// for a view type. A list whose pool has been given a different size should be given
    /// the same number here; 0 tears every view down as it is recycled.
    /// </summary>
    /// <typeparam name="TItemsView">The type of the list.</typeparam>
    /// <param name="itemsView">The list.</param>
    /// <param name="limit">The number of recycled item views that keep their bindings.</param>
    /// <returns>The list, for chaining.</returns>
    public static TItemsView RecycledItemViewLimit<TItemsView>(this TItemsView itemsView, int limit)
        where TItemsView : ItemsView
    {
        ArgumentNullException.ThrowIfNull(itemsView);
        ArgumentOutOfRangeException.ThrowIfNegative(limit);

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
