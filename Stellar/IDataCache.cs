namespace Stellar;

public interface IDataCache
{
    Task StoreAsync<T>(T item, string? cacheKey = null, string? groupKey = null);

    Task StoreAsync<T>(T item, Func<T, string>? cacheKey = null, string? groupKey = null);

    Task StoreManyAsync<T>(IEnumerable<T> items, string? groupKey = null);

    Task StoreManyAsync<T>(IEnumerable<T> items, Func<T, string>? cacheKey = null, string? groupKey = null);

    Task<T?> RetrieveAsync<T>(string? cacheKey = null, string? groupKey = null);

    Task<IEnumerable<T>> RetrieveManyAsync<T>(string groupKey);

    Task<bool> RemoveAsync<T>(string? cacheKey = null, string? groupKey = null);

    Task ClearCacheAsync(string? groupKey = null);
}
