using System.Reactive.Linq;

namespace Stellar.Maui;

public static class CollectionViewExtensions
{
    public static IObservable<T> SelectedItem<T>(this CollectionView collectionView, bool deselect = false)
        where T : class
    {
        var observable =
            collectionView
                .WhenAnyValue(x => x.SelectedItem)
                .IsNotNull()
                .OfType<T>()
                .IsNotNull();

        if (deselect)
        {
            observable
                .Do(
                    _ =>
                    {
                        if (deselect)
                        {
                            collectionView.SelectedItem = null;
                        }
                    });
        }

        return observable;
    }

    public static IObservable<T> SelectedItems<T>(this CollectionView collectionView, bool deselect = false)
        where T : class
    {
        var observable =
            collectionView
                .WhenAnyValue(x => x.SelectedItems)
                .Select(x => x.OfType<T>().ToList())
                .IsNotNull()
                .OfType<T>()
                .IsNotNull();

        if (deselect)
        {
            observable
                .Do(
                    _ =>
                    {
                        if (deselect)
                        {
                            collectionView.SelectedItems = null;
                        }
                    });
        }

        return observable;
    }
}
