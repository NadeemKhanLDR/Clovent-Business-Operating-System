using Clovent.Authentication.Application.Credentials.Commands;
using Clovent.Authentication.Application.Tests.TestSupport;
using Clovent.Authentication.Credentials;
using Clovent.Identity.Users;
using Xunit;

namespace Clovent.Authentication.Application.Tests.Credentials;

public class RecordCredentialCheckCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_Succeeded_ResetsFailedAttempts()
    {
        var repository = new FakeUserCredentialsRepository();
        var userId = UserId.New();
        var credentials = UserCredentials.Create(userId, Now);
        credentials.RecordFailedAttempt();
        credentials.RecordFailedAttempt();
        await repository.AddAsync(credentials);
        var handler = new RecordCredentialCheckCommandHandler(repository);

        await handler.Handle(new RecordCredentialCheckCommand(userId.Value, Succeeded: true), CancellationToken.None);

        Assert.Equal(0, credentials.FailedAttempts.Count);
    }

    [Fact]
    public async Task Handle_NotSucceeded_IncrementsFailedAttempts()
    {
        var repository = new FakeUserCredentialsRepository();
        var userId = UserId.New();
        var credentials = UserCredentials.Create(userId, Now);
        await repository.AddAsync(credentials);
        var handler = new RecordCredentialCheckCommandHandler(repository);

        await handler.Handle(new RecordCredentialCheckCommand(userId.Value, Succeeded: false), CancellationToken.None);

        Assert.Equal(1, credentials.FailedAttempts.Count);
    }

    [Fact]
    public async Task Handle_UnknownUser_ThrowsNotFoundException()
    {
        var repository = new FakeUserCredentialsRepository();
        var handler = new RecordCredentialCheckCommandHandler(repository);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new RecordCredentialCheckCommand(Guid.NewGuid(), Succeeded: true), CancellationToken.None));
    }
}
