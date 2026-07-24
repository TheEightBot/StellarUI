// StellarUI continues to support ListView for as long as .NET MAUI ships it. MAUI marks
// ListView and its cell types obsolete in favour of CollectionView, but removing this
// surface would break every consumer still using it, so the deprecation is suppressed here
// rather than propagated. Revisit when MAUI actually removes the types.
#pragma warning disable CS0618 // Type or member is obsolete

using System.Collections.Concurrent;

namespace Stellar.Maui.Views;

public class ActivatableListView : ListView
{
    private readonly ConcurrentDictionary<Cell, IDisposable> _cellActivators = new();

    // Null until SetCellActivationAction is called, and null again once the returned
    // subscription is disposed, which SetupContent already accounts for.
    private Action<CompositeDisposable, Cell, int>? _cellActivatedAction;

    public ActivatableListView()
        : this(ListViewCachingStrategy.RecycleElement)
    {
    }

    public ActivatableListView(ListViewCachingStrategy cachingStrategy = ListViewCachingStrategy.RecycleElement)
        : base(cachingStrategy)
    {
    }

    public IDisposable SetCellActivationAction(Action<CompositeDisposable, Cell, int> cellActivatedAction)
    {
        _cellActivatedAction = cellActivatedAction;

        return Disposable.Create(
            this,
            x =>
            {
                x._cellActivatedAction = null;

                foreach (var cellItem in x._cellActivators)
                {
                    cellItem.Value?.Dispose();
                }

                x._cellActivators.Clear();
            });
    }

    protected override void SetupContent(Cell content, int index)
    {
        base.SetupContent(content, index);

        if (_cellActivatedAction == null || _cellActivators.ContainsKey(content))
        {
            return;
        }

        var disposable = new CompositeDisposable();
        _cellActivatedAction(disposable, content, index);
        _cellActivators.AddOrUpdate(content, disposable, (k, v) => disposable);
    }

    protected override void UnhookContent(Cell content)
    {
        if (_cellActivators.TryRemove(content, out var disposable))
        {
            disposable?.Dispose();
        }

        base.UnhookContent(content);
    }
}

public static class ActivatableListViewExtensions
{
    public static IDisposable WhenCellActivated(this ActivatableListView reactiveList, Action<CompositeDisposable, Cell, int> whenCellActivated)
    {
        return reactiveList.SetCellActivationAction(whenCellActivated);
    }
}

#pragma warning restore CS0618
