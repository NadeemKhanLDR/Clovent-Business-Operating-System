using Clovent.Authentication.Application;

namespace Clovent.Authentication.Application.Tests.TestSupport;

internal sealed class FakeIdentityUserService(IEnumerable<Guid> activeUserIds) : IIdentityUserService
{
    private readonly HashSet<Guid> _activeUserIds = [.. activeUserIds];

    public List<Guid> LockedUserIds { get; } = [];

    public Task<bool> IsUserActiveAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_activeUserIds.Contains(userId));

    public Task LockUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _activeUserIds.Remove(userId);
        LockedUserIds.Add(userId);
        return Task.CompletedTask;
    }

    public List<Guid> UnlockedUserIds { get; } = [];

    public Task UnlockUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _activeUserIds.Add(userId);
        UnlockedUserIds.Add(userId);
        return Task.CompletedTask;
    }
}
