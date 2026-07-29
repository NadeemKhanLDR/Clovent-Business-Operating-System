using Clovent.Identity.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Clovent.Identity.Infrastructure.Tests.Caching;

public class MemoryPermissionCacheTests
{
    private static MemoryPermissionCache CreateCache() => new(new MemoryCache(new MemoryCacheOptions()));

    [Fact]
    public async Task GetAsync_BeforeSet_ReturnsNull()
    {
        var cache = CreateCache();

        Assert.Null(await cache.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsTheSameCodes()
    {
        var cache = CreateCache();
        var userId = Guid.NewGuid();
        string[] codes = ["module.restaurantpos", "feature.export.excel"];

        await cache.SetAsync(userId, codes);
        var result = await cache.GetAsync(userId);

        Assert.Equal(codes, result);
    }

    [Fact]
    public async Task InvalidateAsync_RemovesCachedValue()
    {
        var cache = CreateCache();
        var userId = Guid.NewGuid();
        await cache.SetAsync(userId, ["module.restaurantpos"]);

        await cache.InvalidateAsync(userId);

        Assert.Null(await cache.GetAsync(userId));
    }

    [Fact]
    public async Task Cache_IsPerUser()
    {
        var cache = CreateCache();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        await cache.SetAsync(userA, ["module.restaurantpos"]);

        Assert.Null(await cache.GetAsync(userB));
    }
}
