using Clovent.Authentication.Application;

namespace Clovent.Desktop.Tests.TestSupport;

internal sealed class FakeIdentityUserService(FakeUserRepository userRepository) : IIdentityUserService
{
    public List<Guid> LockedUserIds { get; } = [];

    public async Task<bool> IsUserActiveAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(new Clovent.Identity.Users.UserId(userId), cancellationToken);
        return user?.Status == Clovent.Identity.Users.UserStatus.Active;
    }

    public Task LockUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        LockedUserIds.Add(userId);
        return Task.CompletedTask;
    }

    public List<Guid> UnlockedUserIds { get; } = [];

    public Task UnlockUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        UnlockedUserIds.Add(userId);
        return Task.CompletedTask;
    }
}
