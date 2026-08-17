using Clovent.Authentication.Application.Credentials.Commands;
using Clovent.Authentication.Application.Tests.TestSupport;
using Clovent.Authentication.Credentials;
using Clovent.Identity.Users;
using Xunit;

namespace Clovent.Authentication.Application.Tests.Credentials;

public class PasswordCommandTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private const string ValidNewPassword = "NewPass1!";
    private const string WeakPassword = "weak";

    private static (UserId UserId, UserCredentials Credentials, FakeUserCredentialsRepository Repository) CreateCredentialsWithPassword(
        FakePasswordHasher hasher, string currentPassword)
    {
        var userId = UserId.New();
        var credentials = UserCredentials.Create(userId, Now);
        credentials.SetPassword(PasswordHash.Create(hasher.Hash(currentPassword)), Now);
        var repository = new FakeUserCredentialsRepository();
        repository.AddAsync(credentials).GetAwaiter().GetResult();
        return (userId, credentials, repository);
    }

    [Fact]
    public async Task ChangePasswordCommandHandler_CorrectCurrentPassword_SetsNewPassword()
    {
        var hasher = new FakePasswordHasher();
        var (userId, credentials, repository) = CreateCredentialsWithPassword(hasher, "OldPass1!");
        var handler = new ChangePasswordCommandHandler(repository, hasher);

        await handler.Handle(new ChangePasswordCommand(userId.Value, "OldPass1!", ValidNewPassword), CancellationToken.None);

        Assert.True(hasher.Verify(ValidNewPassword, credentials.PasswordHash!.Value));
    }

    [Fact]
    public async Task ChangePasswordCommandHandler_WrongCurrentPassword_Throws()
    {
        var hasher = new FakePasswordHasher();
        var (userId, _, repository) = CreateCredentialsWithPassword(hasher, "OldPass1!");
        var handler = new ChangePasswordCommandHandler(repository, hasher);

        await Assert.ThrowsAsync<AuthenticationDomainException>(() =>
            handler.Handle(new ChangePasswordCommand(userId.Value, "WrongPassword1!", ValidNewPassword), CancellationToken.None));
    }

    [Fact]
    public async Task ChangePasswordCommandHandler_WeakNewPassword_Throws()
    {
        var hasher = new FakePasswordHasher();
        var (userId, _, repository) = CreateCredentialsWithPassword(hasher, "OldPass1!");
        var handler = new ChangePasswordCommandHandler(repository, hasher);

        await Assert.ThrowsAsync<AuthenticationDomainException>(() =>
            handler.Handle(new ChangePasswordCommand(userId.Value, "OldPass1!", WeakPassword), CancellationToken.None));
    }

    [Fact]
    public async Task ResetPasswordCommandHandler_ValidRequest_SetsNewPasswordWithoutCurrentPasswordCheck()
    {
        var hasher = new FakePasswordHasher();
        var (userId, credentials, repository) = CreateCredentialsWithPassword(hasher, "OldPass1!");
        var handler = new ResetPasswordCommandHandler(repository, hasher);

        await handler.Handle(new ResetPasswordCommand(userId.Value, ValidNewPassword), CancellationToken.None);

        Assert.True(hasher.Verify(ValidNewPassword, credentials.PasswordHash!.Value));
    }

    [Fact]
    public async Task ResetPasswordCommandHandler_NoExistingCredentials_CreatesThemAndSetsPassword()
    {
        var hasher = new FakePasswordHasher();
        var userId = UserId.New();
        var repository = new FakeUserCredentialsRepository();
        var handler = new ResetPasswordCommandHandler(repository, hasher);

        await handler.Handle(new ResetPasswordCommand(userId.Value, ValidNewPassword), CancellationToken.None);

        var credentials = await repository.GetByUserIdAsync(userId);
        Assert.NotNull(credentials);
        Assert.True(hasher.Verify(ValidNewPassword, credentials!.PasswordHash!.Value));
    }

    [Fact]
    public async Task ResetPasswordCommandHandler_WeakNewPassword_Throws()
    {
        var hasher = new FakePasswordHasher();
        var (userId, _, repository) = CreateCredentialsWithPassword(hasher, "OldPass1!");
        var handler = new ResetPasswordCommandHandler(repository, hasher);

        await Assert.ThrowsAsync<AuthenticationDomainException>(() =>
            handler.Handle(new ResetPasswordCommand(userId.Value, WeakPassword), CancellationToken.None));
    }

    [Fact]
    public async Task UnlockUserCommandHandler_ValidRequest_ResetsFailedAttemptsAndUnlocksIdentityUser()
    {
        var hasher = new FakePasswordHasher();
        var (userId, credentials, repository) = CreateCredentialsWithPassword(hasher, "OldPass1!");
        credentials.RecordFailedAttempt();
        credentials.RecordFailedAttempt();
        var identityService = new FakeIdentityUserService([]);
        var handler = new UnlockUserCommandHandler(repository, identityService);

        await handler.Handle(new UnlockUserCommand(userId.Value), CancellationToken.None);

        Assert.Equal(0, credentials.FailedAttempts.Count);
        Assert.Contains(userId.Value, identityService.UnlockedUserIds);
    }
}
