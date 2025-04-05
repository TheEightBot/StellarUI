using System.Collections;

namespace Stellar.Maui;

public static class ItemsViewExtensions
{
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
