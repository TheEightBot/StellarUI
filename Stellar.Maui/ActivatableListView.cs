using System.Collections.Concurrent;

namespace Stellar.Maui;

public class ActivatableListView : ListView
{
    private readonly ConcurrentDictionary<Cell, IDisposable> _cellActivators = new();

    private Action<CompositeDisposable, Cell, int> _cellActivatedAction;

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
