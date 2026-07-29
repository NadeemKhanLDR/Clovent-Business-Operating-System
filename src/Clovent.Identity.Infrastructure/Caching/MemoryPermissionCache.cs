using Clovent.Identity.Application.Authorization;
using Microsoft.Extensions.Caching.Memory;

namespace Clovent.Identity.Infrastructure.Caching;

/// <summary>
/// <see cref="IPermissionCache"/> implementation over <see cref="IMemoryCache"/> -
/// an in-process cache is sufficient for a single desktop process (no
/// multi-instance invalidation problem to solve, unlike a web farm).
/// </summary>
public sealed class MemoryPermissionCache(IMemoryCache cache) : IPermissionCache
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);

    private static string KeyFor(Guid userId) => $"authorization:permissions:{userId}";

    /// <inheritdoc/>
    public Task<IReadOnlyCollection<string>?> GetAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(cache.TryGetValue(KeyFor(userId), out IReadOnlyCollection<string>? value) ? value : null);

    /// <inheritdoc/>
    public Task SetAsync(Guid userId, IReadOnlyCollection<string> permissionCodes, CancellationToken cancellationToken = default)
    {
        cache.Set(KeyFor(userId), permissionCodes, Ttl);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task InvalidateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        cache.Remove(KeyFor(userId));
        return Task.CompletedTask;
    }
}
