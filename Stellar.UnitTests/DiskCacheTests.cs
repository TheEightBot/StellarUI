using Stellar.DiskDataCache;

namespace Stellar.UnitTests;

/// <summary>
/// DiskCache writes JSON files under a per-group directory. Each test gets its own
/// temporary root so runs cannot interfere with one another or leave state behind.
/// </summary>
public sealed class DiskCacheTests : IDisposable
{
    private readonly string _root;
    private readonly DiskCache _cache;

    public DiskCacheTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "stellar-diskcache-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        _cache = new DiskCache(_root);
    }

    public void Dispose()
    {
        _cache.Dispose();

        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task StoreThenRetrieve_RoundTripsTheValue()
    {
        var item = new Widget("bolt", 42);

        // The (string?) cast is required: StoreAsync's string and Func<T,string>
        // overloads are both optional and nullable, so StoreAsync(item) alone is
        // ambiguous and does not compile.
        await _cache.StoreAsync(item, (string?)null);
        var retrieved = await _cache.RetrieveAsync<Widget>();

        Assert.Equal(item, retrieved);
    }

    [Fact]
    public async Task Retrieve_WithNothingStored_ReturnsDefault()
    {
        var retrieved = await _cache.RetrieveAsync<Widget>();

        Assert.Null(retrieved);
    }

    [Fact]
    public async Task StoreAsync_WithoutAKey_KeysByTypeName()
    {
        // The default key is typeof(T).Name, so a second store of the same type
        // overwrites rather than accumulating.
        await _cache.StoreAsync(new Widget("first", 1), (string?)null);
        await _cache.StoreAsync(new Widget("second", 2), (string?)null);

        var retrieved = await _cache.RetrieveAsync<Widget>();

        Assert.Equal(new Widget("second", 2), retrieved);
    }

    [Fact]
    public async Task DistinctCacheKeys_AreStoredSeparately()
    {
        await _cache.StoreAsync(new Widget("a", 1), cacheKey: "one");
        await _cache.StoreAsync(new Widget("b", 2), cacheKey: "two");

        Assert.Equal(new Widget("a", 1), await _cache.RetrieveAsync<Widget>("one"));
        Assert.Equal(new Widget("b", 2), await _cache.RetrieveAsync<Widget>("two"));
    }

    [Fact]
    public async Task GroupKeys_IsolateEntriesWithTheSameCacheKey()
    {
        await _cache.StoreAsync(new Widget("left", 1), cacheKey: "shared", groupKey: "groupA");
        await _cache.StoreAsync(new Widget("right", 2), cacheKey: "shared", groupKey: "groupB");

        Assert.Equal(new Widget("left", 1), await _cache.RetrieveAsync<Widget>("shared", "groupA"));
        Assert.Equal(new Widget("right", 2), await _cache.RetrieveAsync<Widget>("shared", "groupB"));
    }

    [Fact]
    public async Task StoreManyThenRetrieveMany_ReturnsEveryItemInTheGroup()
    {
        var items = new[]
        {
            new Widget("a", 1),
            new Widget("b", 2),
            new Widget("c", 3),
        };

        await _cache.StoreManyAsync(items, static w => w.Name, "widgets");
        var retrieved = (await _cache.RetrieveManyAsync<Widget>("widgets")).ToList();

        Assert.Equal(3, retrieved.Count);
        Assert.Equal(items.OrderBy(w => w.Name), retrieved.OrderBy(w => w.Name));
    }

    [Fact]
    public async Task RetrieveMany_ForAnUnknownGroup_IsEmpty()
    {
        var retrieved = await _cache.RetrieveManyAsync<Widget>("never-written");

        Assert.Empty(retrieved);
    }

    [Fact]
    public async Task RemoveAsync_DeletesTheEntryAndReportsSuccess()
    {
        await _cache.StoreAsync(new Widget("gone", 1), cacheKey: "target");

        var removed = await _cache.RemoveAsync<Widget>("target");

        Assert.True(removed);
        Assert.Null(await _cache.RetrieveAsync<Widget>("target"));
    }

    [Fact]
    public async Task RemoveAsync_ForAMissingEntry_StillReportsSuccess()
    {
        // Documents actual behaviour rather than the intuitive reading: the bool
        // means "no IO error", not "an entry was removed". File.Delete does not
        // throw for a missing path, so this returns true. Worth knowing before
        // branching on the result.
        var removed = await _cache.RemoveAsync<Widget>("absent");

        Assert.True(removed);
    }

    [Fact]
    public async Task ClearCacheAsync_EmptiesTheGroup()
    {
        await _cache.StoreAsync(new Widget("a", 1), cacheKey: "one", groupKey: "doomed");
        await _cache.StoreAsync(new Widget("b", 2), cacheKey: "two", groupKey: "doomed");

        await _cache.ClearCacheAsync("doomed");

        Assert.Empty(await _cache.RetrieveManyAsync<Widget>("doomed"));
    }

    [Fact]
    public async Task ClearCacheAsync_LeavesOtherGroupsAlone()
    {
        await _cache.StoreAsync(new Widget("keep", 1), cacheKey: "k", groupKey: "survivor");
        await _cache.StoreAsync(new Widget("drop", 2), cacheKey: "k", groupKey: "doomed");

        await _cache.ClearCacheAsync("doomed");

        Assert.Equal(new Widget("keep", 1), await _cache.RetrieveAsync<Widget>("k", "survivor"));
    }

    private sealed record Widget(string Name, int Count);
}
