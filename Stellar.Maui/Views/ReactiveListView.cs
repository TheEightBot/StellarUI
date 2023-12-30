using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls;

namespace Stellar.Maui.Views;

public class ReactiveListView : ListView
{
    private readonly ConcurrentDictionary<Cell, IDisposable> _cellActivators = new ConcurrentDictionary<Cell, IDisposable>();

    private Action<CompositeDisposable, Cell, int> _cellActivatedAction;

    public ReactiveListView(Type cellType, ListViewCachingStrategy cachingStrategy = ListViewCachingStrategy.RecycleElement)
        : this(cachingStrategy)
    {
        ItemTemplate = new DataTemplate(cellType);
    }

    public ReactiveListView(Func<object> loadTemplate, ListViewCachingStrategy cachingStrategy = ListViewCachingStrategy.RecycleElement)
        : this(cachingStrategy)
    {
        ItemTemplate = new DataTemplate(loadTemplate);
    }

    public ReactiveListView(ListViewCachingStrategy cachingStrategy = ListViewCachingStrategy.RecycleElement)
        : base(cachingStrategy)
    {
    }

    public IDisposable SetCellActivationAction(Action<CompositeDisposable, Cell, int> cellActivatedAction)
    {
        _cellActivatedAction = cellActivatedAction;

        return Disposable.Create(
            () =>
            {
                _cellActivatedAction = null;

                foreach (var cellItem in _cellActivators)
                {
                    cellItem.Value?.Dispose();
                }

                _cellActivators.Clear();
            });
    }

    protected override void SetupContent(Cell content, int index)
    {
        base.SetupContent(content, index);

        if (_cellActivatedAction is not null && !_cellActivators.ContainsKey(content))
        {
            var disposable = new CompositeDisposable();
            _cellActivatedAction(disposable, content, index);
            _cellActivators.AddOrUpdate(content, disposable, (_, _) => disposable);
        }
    }

    protected override void UnhookContent(Cell content)
    {
        if (_cellActivators.ContainsKey(content) && _cellActivators.TryRemove(content, out var disposable))
        {
            disposable?.Dispose();
        }

        base.UnhookContent(content);
    }

    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        if (args.NewHandler is null || _cellActivators.IsEmpty)
        {
            return;
        }

        foreach (var key in _cellActivators.Keys.ToArray())
        {
            if (_cellActivators.TryRemove(key, out var disposable))
            {
                disposable?.Dispose();
            }
        }
    }
}

public static class ReactiveListViewExtensions
{
    public static IDisposable WhenCellActivated(this ReactiveListView reactiveList, Action<CompositeDisposable, Cell, int> whenCellActivated)
    {
        return reactiveList.SetCellActivationAction(whenCellActivated);
    }
}
