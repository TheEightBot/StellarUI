using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls;

namespace EightBot.Stellar.Maui.Views;

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

    public IObservable<T> ListViewItemTapped<T>()
        where T : class
    {
        return
            Observable
                .FromEvent<EventHandler<ItemTappedEventArgs>, ItemTappedEventArgs>(
                    static eventHandler =>
                    {
                        void Handler(object sender, ItemTappedEventArgs e) => eventHandler?.Invoke(e);
                        return Handler;
                    },
                    x => this.ItemTapped += x,
                    x => this.ItemTapped -= x)
                .Select(static x => x.Item)
                .OfType<T>();
    }

    public IObservable<object> ListViewItemTapped()
    {
        return
            Observable
                .FromEvent<EventHandler<ItemTappedEventArgs>, ItemTappedEventArgs>(
                    static eventHandler =>
                    {
                        void Handler(object sender, ItemTappedEventArgs e) => eventHandler?.Invoke(e);
                        return Handler;
                    },
                    x => this.ItemTapped += x,
                    x => this.ItemTapped -= x)
                .Select(static args => args.Item);
    }

    public IObservable<T> ListViewItemSelected<T>()
        where T : class
    {
        return
            Observable
                .FromEvent<EventHandler<SelectedItemChangedEventArgs>, SelectedItemChangedEventArgs>(
                    static eventHandler =>
                    {
                        void Handler(object sender, SelectedItemChangedEventArgs e) => eventHandler?.Invoke(e);
                        return Handler;
                    },
                    x => this.ItemSelected += x,
                    x => this.ItemSelected -= x)
                .Select(static x => x.SelectedItem)
                .OfType<T>();
    }

    public IObservable<object> ListViewItemSelected()
    {
        return
            Observable
                .FromEvent<EventHandler<SelectedItemChangedEventArgs>, SelectedItemChangedEventArgs>(
                    eventHandler =>
                    {
                        void Handler(object sender, SelectedItemChangedEventArgs e) => eventHandler?.Invoke(e);
                        return Handler;
                    },
                    x => this.ItemSelected += x,
                    x => this.ItemSelected -= x)
                .Select(static args => args.SelectedItem);
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

        if (_cellActivatedAction != null && !_cellActivators.ContainsKey(content))
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
                disposable = null;
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