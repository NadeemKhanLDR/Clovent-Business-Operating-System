using Clovent.Authentication.Credentials;
using Clovent.Authentication.Credentials.Events;
using Clovent.Identity.Users;
using Xunit;

namespace Clovent.Authentication.Tests.Credentials;

public class UserCredentialsTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static UserCredentials CreateCredentials(DateTimeOffset? now = null) =>
        UserCredentials.Create(UserId.New(), now ?? Now);

    [Fact]
    public void Create_HasNoPasswordOrPinAndRaisesUserCredentialsCreated()
    {
        var credentials = CreateCredentials();

        Assert.Null(credentials.PasswordHash);
        Assert.Null(credentials.PinHash);
        Assert.Equal(PasswordHistory.Empty, credentials.PasswordHistory);
        Assert.Equal(FailedAttempts.Zero, credentials.FailedAttempts);
        Assert.IsType<UserCredentialsCreated>(Assert.Single(credentials.DomainEvents));
    }

    [Fact]
    public void SetPassword_UpdatesHashHistoryAndSecurityStamp()
    {
        var credentials = CreateCredentials();
        var originalStamp = credentials.SecurityStamp;
        credentials.ClearDomainEvents();
        var hash = PasswordHash.Create("hash-1");

        credentials.SetPassword(hash, Now);

        Assert.Equal(hash, credentials.PasswordHash);
        Assert.True(credentials.PasswordHistory.Contains(hash));
        Assert.NotEqual(originalStamp, credentials.SecurityStamp);
        Assert.IsType<PasswordChanged>(Assert.Single(credentials.DomainEvents));
    }

    [Fact]
    public void SetPassword_Null_Throws()
    {
        var credentials = CreateCredentials();

        Assert.Throws<ArgumentNullException>(() => credentials.SetPassword(null!, Now));
    }

    [Fact]
    public void SetPin_UpdatesHashAndSecurityStamp()
    {
        var credentials = CreateCredentials();
        var originalStamp = credentials.SecurityStamp;
        credentials.ClearDomainEvents();
        var hash = PinHash.Create("pin-hash-1");

        credentials.SetPin(hash, Now);

        Assert.Equal(hash, credentials.PinHash);
        Assert.NotEqual(originalStamp, credentials.SecurityStamp);
        Assert.IsType<PinChanged>(Assert.Single(credentials.DomainEvents));
    }

    [Fact]
    public void SetPin_Null_Throws()
    {
        var credentials = CreateCredentials();

        Assert.Throws<ArgumentNullException>(() => credentials.SetPin(null!, Now));
    }

    [Fact]
    public void RecordFailedAttempt_IncrementsCountAndDoesNotRaiseEvent()
    {
        var credentials = CreateCredentials();
        credentials.ClearDomainEvents();

        credentials.RecordFailedAttempt();
        credentials.RecordFailedAttempt();

        Assert.Equal(2, credentials.FailedAttempts.Count);
        Assert.Empty(credentials.DomainEvents);
    }

    [Fact]
    public void ResetFailedAttempts_ReturnsCountToZero()
    {
        var credentials = CreateCredentials();
        credentials.RecordFailedAttempt();
        credentials.RecordFailedAttempt();

        credentials.ResetFailedAttempts();

        Assert.Equal(0, credentials.FailedAttempts.Count);
    }
}
