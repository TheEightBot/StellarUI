using System.Runtime.CompilerServices;

namespace Stellar;

/// <summary>
/// A serial disposable that maintains a weak reference to its contained disposable.
/// The lifetime of the disposable is tied to a key object, allowing it to be garbage collected
/// when the key object is no longer referenced.
/// </summary>
public sealed class WeakSerialDisposable : IDisposable
{
    private readonly ConditionalWeakTable<object, DisposableContainer> _table;
    private readonly object _gate = new object();
    private readonly WeakReference<object> _lifetimeScope;
    private bool _isDisposed;

    /// <summary>
    /// Gets a value indicating whether this instance has been disposed.
    /// </summary>
    public bool IsDisposed => Volatile.Read(ref _isDisposed);

    /// <summary>
    /// Initializes a new instance of the <see cref="WeakSerialDisposable"/> class.
    /// The disposable will be automatically collected when the specified key object is garbage collected.
    /// </summary>
    /// <param name="lifetimeScope">The object that controls the lifetime of the disposable.</param>
    public WeakSerialDisposable(object lifetimeScope)
    {
        if (lifetimeScope == null)
        {
            throw new ArgumentNullException(nameof(lifetimeScope));
        }

        this._table = new ConditionalWeakTable<object, DisposableContainer>();
        this._table.Add(lifetimeScope, new DisposableContainer());
        this._lifetimeScope = new WeakReference<object>(lifetimeScope);
    }

    /// <summary>
    /// Gets or sets the underlying disposable. Setting this disposes the previous disposable.
    /// </summary>
    public IDisposable? Disposable
    {
        get
        {
            lock (_gate)
            {
                if (_isDisposed)
                {
                    return System.Reactive.Disposables.Disposable.Empty;
                }

                if (TryGetContainer(out var container))
                {
                    return container!.Disposable;
                }

                return System.Reactive.Disposables.Disposable.Empty;
            }
        }

        set
        {
            IDisposable? old = null;
            bool shouldDispose = false;

            lock (_gate)
            {
                if (_isDisposed)
                {
                    shouldDispose = true;
                }
                else if (TryGetContainer(out var container))
                {
                    old = container!.SwapDisposable(value);
                }
                else
                {
                    // If we can't get the container, it means the key object has been collected
                    // This WeakSerialDisposable is effectively disposed
                    _isDisposed = true;
                    shouldDispose = true;
                }
            }

            // Dispose outside of lock
            old?.Dispose();

            if (shouldDispose)
            {
                value?.Dispose();
            }
        }
    }

    /// <summary>
    /// Disposes the underlying disposable and marks this instance as disposed.
    /// </summary>
    public void Dispose()
    {
        IDisposable? disposable = null;

        lock (_gate)
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            if (TryGetContainer(out var container))
            {
                disposable = container!.Disposable;
                container.Clear();
            }
        }

        // Dispose outside of lock to avoid deadlocks
        disposable?.Dispose();
    }

    /// <summary>
    /// Attempts to get the container of the disposable if the key object is still alive.
    /// </summary>
    private bool TryGetContainer(out DisposableContainer? container)
    {
        // Use the weak reference to the lifetime scope object
        if (_lifetimeScope.TryGetTarget(out var scope) && scope != null)
        {
            if (_table.TryGetValue(scope, out container))
            {
                return true;
            }
        }

        container = null;
        return false;
    }

    /// <summary>
    /// A container for a single disposable associated with a key object in the ConditionalWeakTable.
    /// </summary>
    private class DisposableContainer
    {
        private IDisposable? _disposable;

        public IDisposable? Disposable => _disposable;

        public IDisposable? SwapDisposable(IDisposable? value)
        {
            return Interlocked.Exchange(ref _disposable, value);
        }

        public void Clear()
        {
            _disposable = null;
        }
    }
}
