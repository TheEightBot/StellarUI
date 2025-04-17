using System.Runtime.CompilerServices;

namespace Stellar;

/// <summary>
/// A single-assignment disposable that maintains a weak reference to its contained disposable.
/// The lifetime of the disposable is tied to a key object, allowing it to be garbage collected
/// when the key object is no longer referenced.
/// </summary>
public sealed class WeakSingleAssignmentDisposable : IDisposable
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
    /// Initializes a new instance of the <see cref="WeakSingleAssignmentDisposable"/> class.
    /// The disposable will be automatically collected when the specified key object is garbage collected.
    /// </summary>
    /// <param name="lifetimeScope">The object that controls the lifetime of the disposable.</param>
    public WeakSingleAssignmentDisposable(object lifetimeScope)
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
    /// Gets or sets the underlying disposable. Can only be set once.
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
            if (value == null)
            {
                return;
            }

            bool shouldDispose = false;

            lock (_gate)
            {
                if (_isDisposed)
                {
                    shouldDispose = true;
                }
                else if (TryGetContainer(out var container))
                {
                    try
                    {
                        container!.SetDisposable(value);
                        return;
                    }
                    catch (InvalidOperationException)
                    {
                        // Already assigned, we should dispose the new value
                        shouldDispose = true;
                    }
                }
                else
                {
                    // If we can't get the container, it means the key object has been collected
                    // This WeakSingleAssignmentDisposable is effectively disposed
                    _isDisposed = true;
                    shouldDispose = true;
                }
            }

            // Either we're disposed, the key object has been collected, or the disposable was already set
            if (shouldDispose)
            {
                value.Dispose();
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
        private bool _isAssigned;

        public IDisposable? Disposable => _disposable;

        public void SetDisposable(IDisposable value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            // Use a bool flag to track if the disposable has been assigned
            bool wasAssigned = _isAssigned;
            _isAssigned = true;

            if (wasAssigned)
            {
                throw new InvalidOperationException("Disposable is already assigned.");
            }

            _disposable = value;
        }

        public void Clear()
        {
            _disposable = null;
        }
    }
}
