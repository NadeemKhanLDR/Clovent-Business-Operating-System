using Clovent.Authentication.Application.Credentials.Commands;
using Clovent.Authentication.Application.Tests.TestSupport;
using Clovent.Authentication.Credentials;
using Clovent.Identity.Users;
using Xunit;

namespace Clovent.Authentication.Application.Tests.Credentials;

public class PinCommandTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private const string ValidPin = "3690";

    private static (UserId UserId, UserCredentials Credentials, FakeUserCredentialsRepository Repository, FakePinHasher Hasher) CreateUserWithPin(string? pin)
    {
        var userId = UserId.New();
        var credentials = UserCredentials.Create(userId, Now);
        if (pin is not null)
        {
            var hasher = new FakePinHasher();
            credentials.SetPin(PinHash.Create(hasher.Hash(pin)), Now);
        }

        var repository = new FakeUserCredentialsRepository();
        repository.AddAsync(credentials).GetAwaiter().GetResult();
        return (userId, credentials, repository, new FakePinHasher());
    }

    [Fact]
    public async Task SetPinCommandHandler_ValidPin_StoresVerifiableHashNotPlaintext()
    {
        var (userId, credentials, repository, hasher) = CreateUserWithPin(null);
        var handler = new SetPinCommandHandler(repository, hasher);

        await handler.Handle(new SetPinCommand(userId.Value, ValidPin), CancellationToken.None);

        Assert.NotNull(credentials.PinHash);
        Assert.NotEqual(ValidPin, credentials.PinHash.Value);
        Assert.True(hasher.Verify(ValidPin, credentials.PinHash!.Value));
    }

    [Fact]
    public async Task SetPinCommandHandler_UserWithoutCredentialsRow_CreatesRowAndSetsPin()
    {
        var userId = UserId.New();
        var repository = new FakeUserCredentialsRepository();
        var handler = new SetPinCommandHandler(repository, new FakePinHasher());

        await handler.Handle(new SetPinCommand(userId.Value, ValidPin), CancellationToken.None);

        var stored = repository.GetByUserIdAsync(userId).GetAwaiter().GetResult();
        Assert.NotNull(stored);
        Assert.NotNull(stored!.PinHash);
    }

    [Theory]
    [InlineData("12")]        // too short
    [InlineData("1234567")]   // too long
    [InlineData("12ab")]      // non-digits
    [InlineData("1111")]      // repeated digits
    [InlineData("1234")]      // sequential digits
    public async Task SetPinCommandHandler_PolicyViolatingPin_Throws(string weakPin)
    {
        var (userId, _, repository, hasher) = CreateUserWithPin(null);
        var handler = new SetPinCommandHandler(repository, hasher);

        await Assert.ThrowsAsync<AuthenticationDomainException>(() =>
            handler.Handle(new SetPinCommand(userId.Value, weakPin), CancellationToken.None));
    }

    [Fact]
    public async Task SetPinCommandHandler_DuplicatePinAcrossUsers_Throws()
    {
        var (firstUserId, _, repository, hasher) = CreateUserWithPin(null);
        var handler = new SetPinCommandHandler(repository, hasher);
        await handler.Handle(new SetPinCommand(firstUserId.Value, ValidPin), CancellationToken.None);

        var (secondUserId, secondCredentials, _, _) = CreateUserWithPin(null);
        repository.AddAsync(secondCredentials).GetAwaiter().GetResult();

        await Assert.ThrowsAsync<AuthenticationDomainException>(() =>
            handler.Handle(new SetPinCommand(secondUserId.Value, ValidPin), CancellationToken.None));

        Assert.Null(secondCredentials.PinHash);
    }

    [Fact]
    public async Task SetPinCommandHandler_ReassigningOwnPin_DoesNotTripUniqueness()
    {
        var (userId, credentials, repository, hasher) = CreateUserWithPin("4580");

        var handler = new SetPinCommandHandler(repository, hasher);
        await handler.Handle(new SetPinCommand(userId.Value, "5702"), CancellationToken.None);

        Assert.True(hasher.Verify("5702", credentials.PinHash!.Value));
    }

    [Fact]
    public async Task SetPinCommandHandler_NullPin_ClearsExistingPin()
    {
        var (userId, credentials, repository, hasher) = CreateUserWithPin(ValidPin);
        var handler = new SetPinCommandHandler(repository, hasher);

        await handler.Handle(new SetPinCommand(userId.Value, null), CancellationToken.None);

        Assert.Null(credentials.PinHash);
    }

    [Fact]
    public async Task SetPinCommandHandler_WhitespacePin_ClearsExistingPin()
    {
        var (userId, credentials, repository, hasher) = CreateUserWithPin(ValidPin);
        var handler = new SetPinCommandHandler(repository, hasher);

        await handler.Handle(new SetPinCommand(userId.Value, "   "), CancellationToken.None);

        Assert.Null(credentials.PinHash);
    }
}
