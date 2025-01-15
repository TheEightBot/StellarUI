using System.Collections;

namespace Stellar.Maui;

public static class ItemsViewExtensions
{
    public static IDisposable BindItems<TVisual>(this ItemsView<TVisual> itemsView, IObservable<IEnumerable> listItems)
        where TVisual : BindableObject
    {
        var bindingDisposables = new CompositeDisposable();

        listItems
            .ObserveOn(RxApp.MainThreadScheduler)
            .Do(
                items =>
                {
                    if (itemsView == null)
                    {
                        return;
                    }

                    itemsView.ItemsSource = null;
                    itemsView.ItemsSource = items;
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
