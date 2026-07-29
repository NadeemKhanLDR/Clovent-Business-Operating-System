using Clovent.Authentication.Application.LoginAttempts.Commands;
using Clovent.Authentication.Application.Tests.TestSupport;
using Clovent.Authentication.Lockouts;
using Clovent.Authentication.LoginAttempts;
using Xunit;

namespace Clovent.Authentication.Application.Tests.LoginAttempts;

public class RecordLoginAttemptCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_SuccessfulAttempt_PersistsAndDoesNotEvaluateLockout()
    {
        var attempts = new FakeLoginAttemptRepository();
        var userId = Guid.NewGuid();
        var identityUsers = new FakeIdentityUserService([userId]);
        var handler = new RecordLoginAttemptCommandHandler(attempts, identityUsers, new FakeTimeProvider(Now));

        var dto = await handler.Handle(
            new RecordLoginAttemptCommand("ada@example.com", userId, LoginOutcome.Succeeded),
            CancellationToken.None);

        Assert.Equal("Succeeded", dto.Outcome);
        Assert.Empty(identityUsers.LockedUserIds);
    }

    [Fact]
    public async Task Handle_FailuresBelowThreshold_DoesNotLockUser()
    {
        var attempts = new FakeLoginAttemptRepository();
        var userId = Guid.NewGuid();
        var identityUsers = new FakeIdentityUserService([userId]);
        var policy = LockoutPolicy.Create(5, TimeSpan.FromMinutes(15));
        var handler = new RecordLoginAttemptCommandHandler(attempts, identityUsers, new FakeTimeProvider(Now), policy);

        for (var i = 0; i < 4; i++)
        {
            await handler.Handle(
                new RecordLoginAttemptCommand("ada@example.com", userId, LoginOutcome.InvalidCredentials),
                CancellationToken.None);
        }

        Assert.Empty(identityUsers.LockedUserIds);
    }

    [Fact]
    public async Task Handle_FailuresReachThreshold_LocksUserViaIdentityUserService()
    {
        var attempts = new FakeLoginAttemptRepository();
        var userId = Guid.NewGuid();
        var identityUsers = new FakeIdentityUserService([userId]);
        var policy = LockoutPolicy.Create(5, TimeSpan.FromMinutes(15));
        var handler = new RecordLoginAttemptCommandHandler(attempts, identityUsers, new FakeTimeProvider(Now), policy);

        for (var i = 0; i < 5; i++)
        {
            await handler.Handle(
                new RecordLoginAttemptCommand("ada@example.com", userId, LoginOutcome.InvalidCredentials),
                CancellationToken.None);
        }

        Assert.Contains(userId, identityUsers.LockedUserIds);
    }

    [Fact]
    public async Task Handle_UserAlreadyInactive_DoesNotAttemptLock()
    {
        var attempts = new FakeLoginAttemptRepository();
        var userId = Guid.NewGuid();
        var identityUsers = new FakeIdentityUserService([]); // not active
        var policy = LockoutPolicy.Create(1, TimeSpan.FromMinutes(15));
        var handler = new RecordLoginAttemptCommandHandler(attempts, identityUsers, new FakeTimeProvider(Now), policy);

        await handler.Handle(
            new RecordLoginAttemptCommand("ada@example.com", userId, LoginOutcome.InvalidCredentials),
            CancellationToken.None);

        Assert.Empty(identityUsers.LockedUserIds);
    }

    [Fact]
    public async Task Handle_UnknownIdentifier_DoesNotEvaluateLockout()
    {
        var attempts = new FakeLoginAttemptRepository();
        var identityUsers = new FakeIdentityUserService([]);
        var handler = new RecordLoginAttemptCommandHandler(attempts, identityUsers, new FakeTimeProvider(Now));

        var dto = await handler.Handle(
            new RecordLoginAttemptCommand("ghost@example.com", null, LoginOutcome.UserNotFound),
            CancellationToken.None);

        Assert.Null(dto.UserId);
        Assert.Empty(identityUsers.LockedUserIds);
    }
}
