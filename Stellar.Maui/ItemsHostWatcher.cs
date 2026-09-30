using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Stellar.Maui;

/// <summary>
/// Tracks the item views an <see cref="ItemsView"/> has recycled that are still holding
/// their bindings, and releases them when the list leaves its window.
/// </summary>
internal sealed class ItemsHostWatcher
{
    // RecyclerView.RecycledViewPool.DEFAULT_MAX_SCRAP. A recycler drops the holders it is
    // handed beyond this many, and a view it dropped is never rebound.
    internal const int DefaultLimit = 5;

    private static readonly ConditionalWeakTable<ItemsView, ItemsHostWatcher> Watchers = new();

    private readonly Lock _gate = new();

    private readonly List<WeakReference<IParkedItemView>> _parked = new(DefaultLimit);

    private int _limit = DefaultLimit;

    private ItemsHostWatcher(ItemsView host)
    {
        host.PropertyChanged += this.OnHostPropertyChanged;
    }

    public static ItemsHostWatcher For(ItemsView host) =>
        Watchers.GetValue(host, static x => new ItemsHostWatcher(x));

    public static ItemsHostWatcher? Find(ItemsView host) =>
        Watchers.TryGetValue(host, out var watcher) ? watcher : null;

    public int Limit
    {
        get => Volatile.Read(ref _limit);
        set => Volatile.Write(ref _limit, value);
    }

    public bool TryPark(WeakReference<IParkedItemView> view)
    {
        lock (_gate)
        {
            for (var i = _parked.Count - 1; i >= 0; i--)
            {
                if (!_parked[i].TryGetTarget(out _))
                {
                    _parked.RemoveAt(i);
                }
            }

            if (_parked.Count >= _limit)
            {
                return false;
            }

            _parked.Add(view);

            return true;
        }
    }

    public void Unpark(WeakReference<IParkedItemView> view)
    {
        lock (_gate)
        {
            _parked.Remove(view);
        }
    }

    public void ReleaseParked()
    {
        WeakReference<IParkedItemView>[] parked;

        lock (_gate)
        {
            if (_parked.Count == 0)
            {
                return;
            }

            parked = _parked.ToArray();
            _parked.Clear();
        }

        foreach (var weak in parked)
        {
            if (weak.TryGetTarget(out var view))
            {
                view.DeactivateParked();
            }
        }
    }

    private void OnHostPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VisualElement.Window) && sender is ItemsView { Window: null })
        {
            this.ReleaseParked();
        }
    }
}
