using Clovent.Identity.Application.Authorization;

namespace Clovent.Identity.Application.Tests.TestSupport;

internal sealed class FakePermissionCache : IPermissionCache
{
    private readonly Dictionary<Guid, IReadOnlyCollection<string>> _cache = [];

    public int GetCallCount { get; private set; }
    public int SetCallCount { get; private set; }

    public Task<IReadOnlyCollection<string>?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        GetCallCount++;
        return Task.FromResult(_cache.GetValueOrDefault(userId));
    }

    public Task SetAsync(Guid userId, IReadOnlyCollection<string> permissionCodes, CancellationToken cancellationToken = default)
    {
        SetCallCount++;
        _cache[userId] = permissionCodes;
        return Task.CompletedTask;
    }

    public Task InvalidateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _cache.Remove(userId);
        return Task.CompletedTask;
    }
}
