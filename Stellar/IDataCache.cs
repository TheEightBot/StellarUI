namespace Stellar;

/// <summary>
/// A keyed store for serialized objects, organized into optional groups.
/// </summary>
public interface IDataCache
{
    /// <summary>
    /// Stores a single item under an explicit key.
    /// </summary>
    /// <typeparam name="T">The type of the item being stored.</typeparam>
    /// <param name="item">The item to store.</param>
    /// <param name="cacheKey">The key to store the item under. When <see langword="null"/>, the name of <typeparamref name="T"/> is used, so a later store of the same type replaces this entry.</param>
    /// <param name="groupKey">The group to store the item in. When <see langword="null"/>, the default group is used.</param>
    /// <returns>A task that completes once the item has been written.</returns>
    Task StoreAsync<T>(T item, string? cacheKey = null, string? groupKey = null);

    /// <summary>
    /// Stores a single item under a key derived from the item itself.
    /// </summary>
    /// <typeparam name="T">The type of the item being stored.</typeparam>
    /// <param name="item">The item to store.</param>
    /// <param name="cacheKey">Produces the key to store the item under. Required — call the <see cref="StoreAsync{T}(T, string, string)"/> overload to key by the type name instead.</param>
    /// <param name="groupKey">The group to store the item in. When <see langword="null"/>, the default group is used.</param>
    /// <returns>A task that completes once the item has been written.</returns>
    Task StoreAsync<T>(T item, Func<T, string> cacheKey, string? groupKey = null);

    /// <summary>
    /// Stores a sequence of items, each under a freshly generated key.
    /// </summary>
    /// <typeparam name="T">The type of the items being stored.</typeparam>
    /// <param name="items">The items to store.</param>
    /// <param name="groupKey">The group to store the items in. When <see langword="null"/>, the default group is used.</param>
    /// <returns>A task that completes once every item has been written.</returns>
    /// <remarks>Because the keys are generated, these entries can only be read back with <see cref="RetrieveManyAsync{T}"/>, and repeated calls accumulate rather than replace.</remarks>
    Task StoreManyAsync<T>(IEnumerable<T> items, string? groupKey = null);

    /// <summary>
    /// Stores a sequence of items, each under a key derived from the item itself.
    /// </summary>
    /// <typeparam name="T">The type of the items being stored.</typeparam>
    /// <param name="items">The items to store.</param>
    /// <param name="cacheKey">Produces the key to store each item under. Required — call the <see cref="StoreManyAsync{T}(IEnumerable{T}, string)"/> overload to generate keys instead.</param>
    /// <param name="groupKey">The group to store the items in. When <see langword="null"/>, the default group is used.</param>
    /// <returns>A task that completes once every item has been written.</returns>
    Task StoreManyAsync<T>(IEnumerable<T> items, Func<T, string> cacheKey, string? groupKey = null);

    /// <summary>
    /// Retrieves a single item by key.
    /// </summary>
    /// <typeparam name="T">The type of the item to retrieve.</typeparam>
    /// <param name="cacheKey">The key the item was stored under. When <see langword="null"/>, the name of <typeparamref name="T"/> is used.</param>
    /// <param name="groupKey">The group to read from. When <see langword="null"/>, the default group is used.</param>
    /// <returns>The stored item, or <see langword="default"/> when no entry exists for the key.</returns>
    Task<T?> RetrieveAsync<T>(string? cacheKey = null, string? groupKey = null);

    /// <summary>
    /// Retrieves every item in a group.
    /// </summary>
    /// <typeparam name="T">The type to deserialize each entry as.</typeparam>
    /// <param name="groupKey">The group to read.</param>
    /// <returns>The items in the group, or an empty sequence when the group is empty or unknown.</returns>
    Task<IEnumerable<T>> RetrieveManyAsync<T>(string groupKey);

    /// <summary>
    /// Removes a single entry by key.
    /// </summary>
    /// <typeparam name="T">The type of the item to remove.</typeparam>
    /// <param name="cacheKey">The key the item was stored under. When <see langword="null"/>, the name of <typeparamref name="T"/> is used.</param>
    /// <param name="groupKey">The group to remove from. When <see langword="null"/>, the default group is used.</param>
    /// <returns><see langword="true"/> when an entry existed and was removed; <see langword="false"/> when no entry existed for the key, or when it could not be removed.</returns>
    Task<bool> RemoveAsync<T>(string? cacheKey = null, string? groupKey = null);

    /// <summary>
    /// Removes every entry in a group.
    /// </summary>
    /// <param name="groupKey">The group to clear. When <see langword="null"/>, the default group is cleared.</param>
    /// <returns>A task that completes once the group has been cleared.</returns>
    Task ClearCacheAsync(string? groupKey = null);
}
