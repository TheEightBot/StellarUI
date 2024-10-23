using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.RateLimiting;

namespace Stellar.DiskDataCache;

public class DiskCache : IDataCache, IDisposable
{
    private const string DefaultCacheFolderName = "Cache";

    private readonly JsonSerializerOptions _serializerOptions;

    private readonly string _appDataDirectory;

    private readonly ConcurrencyLimiter _readLimiter;

    private readonly ConcurrencyLimiter _writeLimiter;

    private readonly object _cacheDirectoryLock = new();

    private bool _isDisposed;

    public DiskCache(string appDataDirectory, JsonSerializerOptions? serializerOptions = null, int maxParallelism = 2)
    {
        _serializerOptions =
            serializerOptions ??
            new JsonSerializerOptions(JsonSerializerDefaults.Web);

        _appDataDirectory = appDataDirectory;

        _readLimiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions { PermitLimit = maxParallelism, QueueLimit = int.MaxValue, QueueProcessingOrder = QueueProcessingOrder.OldestFirst });
        _writeLimiter = new ConcurrencyLimiter(new ConcurrencyLimiterOptions { PermitLimit = 1, QueueLimit = int.MaxValue, QueueProcessingOrder = QueueProcessingOrder.OldestFirst });
    }

    private DirectoryInfo GetCacheDirectory(string? cacheDirectoryName = DefaultCacheFolderName)
    {
        lock (_cacheDirectoryLock)
        {
            if (string.IsNullOrEmpty(cacheDirectoryName))
            {
                cacheDirectoryName = DefaultCacheFolderName;
            }

            var cacheDirectory = Path.Combine(this._appDataDirectory, cacheDirectoryName);

            return
                Directory.Exists(cacheDirectory)
                    ? new DirectoryInfo(cacheDirectory)
                    : Directory.CreateDirectory(cacheDirectory);
        }
    }

    public async Task StoreAsync<T>(T item, string? cacheKey = null, string? groupKey = null)
    {
        await SerializeJsonToFileAsync(groupKey, cacheKey ?? typeof(T).Name, item).ConfigureAwait(false);
    }

    public async Task StoreAsync<T>(T item, Func<T, string>? cacheKey = null, string? groupKey = null)
    {
        await this.SerializeJsonToFileAsync(groupKey, cacheKey?.Invoke(item) ?? typeof(T).Name, item).ConfigureAwait(false);
    }

    public async Task<T?> RetrieveAsync<T>(string? cacheKey = null, string? groupKey = null)
    {
        return await this.DeserializeJsonFromFileAsync<T>(groupKey, cacheKey ?? typeof(T).Name).ConfigureAwait(false);
    }

    public async Task<bool> RemoveAsync<T>(string? cacheKey = null, string? groupKey = null)
    {
        using var lease = await _writeLimiter.AcquireAsync().ConfigureAwait(false);
        try
        {
            RemoveFile(groupKey, cacheKey ?? typeof(T).Name);

            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }

    public async Task ClearCacheAsync(string? groupKey = null)
    {
        using var lease = await _writeLimiter.AcquireAsync().ConfigureAwait(false);

        if (string.IsNullOrEmpty(groupKey))
        {
            groupKey = DefaultCacheFolderName;
        }

        var cacheDirectory = Path.Combine(this._appDataDirectory, groupKey);

        if (Directory.Exists(cacheDirectory))
        {
            Directory.Delete(cacheDirectory, true);
        }
    }

    private async Task<T?> DeserializeJsonFromFileAsync<T>(string cacheDirectoryName, string fileName)
    {
        using var lease = await _readLimiter.AcquireAsync().ConfigureAwait(false);

        await using var fileStream = GetFile(cacheDirectoryName, fileName);

        if (fileStream == Stream.Null)
        {
            return default(T);
        }

        return await JsonSerializer.DeserializeAsync<T>(fileStream, _serializerOptions).ConfigureAwait(false);
    }

    private async Task SerializeJsonToFileAsync<T>(string? cacheDirectoryName, string fileName, T objectToSerialize)
    {
        using var lease = await _writeLimiter.AcquireAsync().ConfigureAwait(false);

        var tmp = $"{fileName}.tmp";

        var cacheDirectory = GetCacheDirectory(cacheDirectoryName);
        var fullTemp = Path.Combine(cacheDirectory.FullName, tmp);
        var fullFinal = Path.Combine(cacheDirectory.FullName, fileName);

        using var stream = CreateFile(cacheDirectoryName, tmp);

        await JsonSerializer.SerializeAsync(stream, objectToSerialize, _serializerOptions).ConfigureAwait(false);

        if (File.Exists(fullFinal))
        {
            File.Delete(fullFinal);
        }

        new FileInfo(fullTemp).MoveTo(fullFinal);
    }

    private Stream CreateFile(string cacheDirectoryName, string fileName)
    {
        var cacheDirectory = GetCacheDirectory(cacheDirectoryName);

        var filePath = Path.Combine(cacheDirectory.FullName, fileName);

        return
            File.Exists(filePath)
                ? new FileStream(filePath, FileMode.Truncate)
                : new FileStream(filePath, FileMode.Create);
    }

    private Stream GetFile(string cacheDirectoryName, string fileName)
    {
        var cacheDirectory = GetCacheDirectory(cacheDirectoryName);

        var filePath = Path.Combine(cacheDirectory.FullName, fileName);

        return File.Exists(filePath)
            ? new FileStream(filePath, FileMode.Open)
            : Stream.Null;
    }

    private void RemoveFile(string cacheDirectoryName, string fileName)
    {
        var cacheDirectory = GetCacheDirectory(cacheDirectoryName);

        var filePath = Path.Combine(cacheDirectory.FullName, fileName);

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    public void Dispose()
    {
        // Dispose of unmanaged resources.
        Dispose(true);

        // Suppress finalization.
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        if (disposing)
        {
            _readLimiter.Dispose();
            _writeLimiter.Dispose();
        }

        _isDisposed = true;
    }
}
