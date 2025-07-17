using System.Collections;
using System.Runtime.CompilerServices;

namespace Stellar;

/// <summary>
/// A disposable container that maintains weak references to the disposables it contains.
/// The lifetime of the disposables is tied to a key object, allowing them to be garbage collected
/// when the key object is no longer referenced.
/// </summary>
public sealed class WeakCompositeDisposable : ICollection<IDisposable>, IDisposable
{
    private readonly ConditionalWeakTable<object, DisposableCollection> _table;
    private readonly object _gate = new object();
    private readonly WeakReference<object> _lifetimeScope;
    private bool _isDisposed;

    /// <summary>
    /// Gets a value indicating whether this instance has been disposed.
    /// </summary>
    public bool IsDisposed => Volatile.Read(ref _isDisposed);

    /// <summary>
    /// Initializes a new instance of the <see cref="WeakCompositeDisposable"/> class.
    /// The disposables will be automatically collected when the specified key object is garbage collected.
    /// </summary>
    /// <param name="lifetimeScope">The object that controls the lifetime of the disposables.</param>
    public WeakCompositeDisposable(object lifetimeScope)
    {
        if (lifetimeScope == null)
        {
            throw new ArgumentNullException(nameof(lifetimeScope));
        }

        this._table = new ConditionalWeakTable<object, DisposableCollection>
        {
            { lifetimeScope, new DisposableCollection() },
        };
        this._lifetimeScope = new WeakReference<object>(lifetimeScope);
    }

    /// <summary>
    /// Gets the number of disposables in this collection.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_gate)
            {
                if (_isDisposed)
                {
                    return 0;
                }

                if (TryGetCollection(out var collection))
                {
                    return collection!.Count;
                }

                return 0;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the collection is read-only.
    /// </summary>
    public bool IsReadOnly => false;

    /// <summary>
    /// Adds a disposable to the composite. If the composite is disposed, the disposable is disposed immediately.
    /// </summary>
    /// <param name="item">The disposable to add.</param>
    public void Add(IDisposable item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        lock (_gate)
        {
            if (!_isDisposed)
            {
                if (TryGetCollection(out var collection))
                {
                    collection!.Add(item);
                    return;
                }

                // If we can't get the collection, it means the key object has been collected
                // This WeakCompositeDisposable is effectively disposed
                _isDisposed = true;
            }
        }

        // Either we're disposed or the key object has been collected
        item.Dispose();
    }

    /// <summary>
    /// Removes a disposable from the composite. If the disposable was in the composite, it is disposed.
    /// </summary>
    /// <param name="item">The disposable to remove.</param>
    /// <returns>true if the disposable was removed successfully; otherwise, false.</returns>
    public bool Remove(IDisposable item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        lock (_gate)
        {
            if (_isDisposed)
            {
                return false;
            }

            if (TryGetCollection(out var collection))
            {
                if (collection!.Remove(item))
                {
                    item.Dispose();
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Removes and disposes all disposables from the composite.
    /// </summary>
    public void Clear()
    {
        List<IDisposable> disposables = new();

        lock (_gate)
        {
            if (_isDisposed)
            {
                return;
            }

            if (TryGetCollection(out var collection))
            {
                // Capture all disposables to dispose outside of the lock
                disposables.AddRange(collection!);
                collection!.Clear();
            }
        }

        // Dispose outside of lock
        foreach (var disposable in disposables)
        {
            disposable.Dispose();
        }
    }

    /// <summary>
    /// Determines whether the composite contains a specific disposable.
    /// </summary>
    /// <param name="item">The disposable to locate.</param>
    /// <returns>true if the disposable is found in the composite; otherwise, false.</returns>
    public bool Contains(IDisposable item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        lock (_gate)
        {
            if (_isDisposed)
            {
                return false;
            }

            if (TryGetCollection(out var collection))
            {
                return collection!.Contains(item);
            }

            return false;
        }
    }

    /// <summary>
    /// Copies the disposables contained in the composite to an array, starting at a particular index.
    /// </summary>
    /// <param name="array">The array to copy to.</param>
    /// <param name="arrayIndex">The index in the array at which copying begins.</param>
    public void CopyTo(IDisposable[] array, int arrayIndex)
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array));
        }

        if (arrayIndex < 0 || arrayIndex >= array.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        }

        lock (_gate)
        {
            if (_isDisposed)
            {
                return;
            }

            if (TryGetCollection(out var collection))
            {
                if (arrayIndex + collection!.Count > array.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(arrayIndex));
                }

                collection!.CopyTo(array, arrayIndex);
            }
        }
    }

    /// <summary>
    /// Disposes all disposables in the composite and marks the composite as disposed.
    /// </summary>
    public void Dispose()
    {
        List<IDisposable> disposables = new();

        lock (_gate)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            if (TryGetCollection(out var collection))
            {
                // Capture all disposables to dispose outside of the lock
                disposables.AddRange(collection!);
                collection!.Clear();
            }
        }

        // Dispose outside of lock to avoid deadlocks
        foreach (var disposable in disposables)
        {
            disposable.Dispose();
        }
    }

    /// <summary>
    /// Returns an enumerator that iterates through the disposables in the composite.
    /// </summary>
    /// <returns>An enumerator for the disposables in the composite.</returns>
    public IEnumerator<IDisposable> GetEnumerator()
    {
        List<IDisposable> snapshot;

        lock (_gate)
        {
            if (_isDisposed)
            {
                return Enumerable.Empty<IDisposable>().GetEnumerator();
            }

            if (!TryGetCollection(out var collection))
            {
                return Enumerable.Empty<IDisposable>().GetEnumerator();
            }

            // Create a snapshot of the disposables
            snapshot = collection!.ToList();
        }

        return snapshot.GetEnumerator();
    }

    /// <summary>
    /// Returns an enumerator that iterates through the disposables in the composite.
    /// </summary>
    /// <returns>An enumerator for the disposables in the composite.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Attempts to get the collection of disposables if the key object is still alive.
    /// </summary>
    private bool TryGetCollection(out DisposableCollection? collection)
    {
        // Use the weak reference to the lifetime scope object
        if (_lifetimeScope.TryGetTarget(out var scope) && scope != null)
        {
            if (_table.TryGetValue(scope, out collection))
            {
                return true;
            }
        }

        collection = null;
        return false;
    }

    /// <summary>
    /// A collection of _disposables associated with a key object in the ConditionalWeakTable.
    /// </summary>
    private class DisposableCollection : IEnumerable<IDisposable>
    {
        private readonly List<IDisposable> _disposables = new();

        public int Count => _disposables.Count;

        public void Add(IDisposable item)
        {
            _disposables.Add(item);
        }

        public bool Remove(IDisposable item)
        {
            return _disposables.Remove(item);
        }

        public bool Contains(IDisposable item)
        {
            return _disposables.Contains(item);
        }

        public void Clear()
        {
            _disposables.Clear();
        }

        public void CopyTo(IDisposable[] array, int arrayIndex)
        {
            _disposables.CopyTo(array, arrayIndex);
        }

        public List<IDisposable> ToList()
        {
            return new List<IDisposable>(_disposables);
        }

        public IEnumerator<IDisposable> GetEnumerator()
        {
            return _disposables.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _disposables.GetEnumerator();
        }
    }
}
