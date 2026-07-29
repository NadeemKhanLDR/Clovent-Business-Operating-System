using Clovent.Authentication.LoginAttempts;
using Clovent.Authentication.LoginAttempts.Events;
using Clovent.Identity.Users;
using Xunit;

namespace Clovent.Authentication.Tests.LoginAttempts;

public class LoginAttemptTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Record_Success_SetsPropertiesAndRaisesEvent()
    {
        var userId = UserId.New();

        var attempt = LoginAttempt.Record("ada@example.com", userId, LoginOutcome.Succeeded, Now);

        Assert.Equal("ada@example.com", attempt.AttemptedIdentifier);
        Assert.Equal(userId, attempt.UserId);
        Assert.Equal(LoginOutcome.Succeeded, attempt.Outcome);
        Assert.False(attempt.IsFailure);
        Assert.IsType<LoginAttemptRecorded>(Assert.Single(attempt.DomainEvents));
    }

    [Theory]
    [InlineData(LoginOutcome.UserNotFound)]
    [InlineData(LoginOutcome.InvalidCredentials)]
    [InlineData(LoginOutcome.UserInactive)]
    [InlineData(LoginOutcome.UserLocked)]
    public void Record_FailureOutcomes_IsFailureIsTrue(LoginOutcome outcome)
    {
        var attempt = LoginAttempt.Record("unknown@example.com", null, outcome, Now);

        Assert.True(attempt.IsFailure);
    }

    [Fact]
    public void Record_UnrecognizedIdentifier_AllowsNullUserId()
    {
        var attempt = LoginAttempt.Record("not-a-real-user", null, LoginOutcome.UserNotFound, Now);

        Assert.Null(attempt.UserId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Record_EmptyIdentifier_Throws(string identifier)
    {
        Assert.Throws<ArgumentException>(() => LoginAttempt.Record(identifier, null, LoginOutcome.UserNotFound, Now));
    }

    [Fact]
    public void Record_IdentifierTooLong_Throws()
    {
        var tooLong = new string('a', 321);

        Assert.Throws<ArgumentException>(() => LoginAttempt.Record(tooLong, null, LoginOutcome.UserNotFound, Now));
    }
}
