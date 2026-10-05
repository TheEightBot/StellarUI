using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Stellar.Maui;

/// <summary>
/// Tracks the item views an <see cref="ItemsView"/> has recycled that are still holding
/// their bindings, and releases them when the list leaves its window.
/// </summary>
internal sealed class ItemsHostWatcher
{
    /// <summary>
    /// Stands in for the watcher of a list that has not opted in, so that an item view can
    /// remember it belongs to such a list without a reference to the list. It keeps
    /// nothing and refuses every view.
    /// </summary>
    public static readonly ItemsHostWatcher None = new();

    private static readonly ConditionalWeakTable<ItemsView, ItemsHostWatcher> Watchers = new();

    // Zero until a list opts in. Read on every bind of every item view, so a list in an
    // app that never opts in does not pay for a table lookup.
    private static int _watched;

    private readonly Lock _gate = new();

    private readonly List<WeakReference<IParkedItemView>> _parked = [];

    private readonly WeakReference<ItemsView>? _host;

    // Zero until the list opts in through RecycledItemViewLimit.
    private int _limit;

    private ItemsHostWatcher()
    {
    }

    private ItemsHostWatcher(ItemsView host)
    {
        _host = new WeakReference<ItemsView>(host);

        host.PropertyChanged += this.OnHostPropertyChanged;
    }

    public static ItemsHostWatcher For(ItemsView host) =>
        Watchers.GetValue(host, static x => Create(x));

    public static ItemsHostWatcher? Find(ItemsView host) =>
        Volatile.Read(ref _watched) != 0 && Watchers.TryGetValue(host, out var watcher) ? watcher : null;

    public int Limit
    {
        get => Volatile.Read(ref _limit);
        set
        {
            var lowered = value < Volatile.Read(ref _limit);

            Volatile.Write(ref _limit, value);

            // Views parked under the old limit would otherwise keep their bindings
            // until they are rebound or the list leaves its window.
            if (lowered)
            {
                this.ReleaseParked();
            }
        }
    }

    public bool IsFor(ItemsView host) =>
        _host is not null && _host.TryGetTarget(out var target) && ReferenceEquals(target, host);

    /// <summary>
    /// Takes a slot for a view if the list is in its window and has one free. The weak
    /// reference the slot is held through is created only then, and is handed back so the
    /// same one can give the slot up again or take another later.
    /// </summary>
    /// <param name="view">The view asking for a slot.</param>
    /// <param name="slot">The weak reference the view was given before, if any; set when a slot is taken.</param>
    /// <returns>True if the view now holds a slot.</returns>
    public bool TryPark(IParkedItemView view, ref WeakReference<IParkedItemView>? slot)
    {
        if (_host is null || !_host.TryGetTarget(out var host) || host.Window is null)
        {
            return false;
        }

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

            slot ??= new WeakReference<IParkedItemView>(view);

            _parked.Add(slot);

            return true;
        }
    }

    public void Unpark(WeakReference<IParkedItemView> slot)
    {
        lock (_gate)
        {
            _parked.Remove(slot);
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

    private static ItemsHostWatcher Create(ItemsView host)
    {
        Interlocked.Increment(ref _watched);

        return new ItemsHostWatcher(host);
    }

    private void OnHostPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(VisualElement.Window) && sender is ItemsView { Window: null })
        {
            this.ReleaseParked();
        }
    }
}
