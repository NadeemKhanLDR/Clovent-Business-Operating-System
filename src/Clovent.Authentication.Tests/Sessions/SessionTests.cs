using Clovent.Authentication;
using Clovent.Authentication.Sessions;
using Clovent.Authentication.Sessions.Events;
using Clovent.Identity.Users;
using Xunit;

namespace Clovent.Authentication.Tests.Sessions;

public class SessionTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(30);

    private static Session StartSession(DateTimeOffset? now = null) =>
        Session.Start(UserId.New(), IdleTimeout, now ?? Now);

    [Fact]
    public void Start_SetsActiveAndRaisesSessionStarted()
    {
        var session = StartSession();

        Assert.Equal(SessionStatus.Active, session.Status);
        Assert.Equal(Now + IdleTimeout, session.ExpiresAtUtc);
        Assert.IsType<SessionStarted>(Assert.Single(session.DomainEvents));
    }

    [Fact]
    public void Start_NonPositiveTimeout_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Session.Start(UserId.New(), TimeSpan.Zero, Now));
    }

    [Fact]
    public void Touch_WhenActive_ExtendsExpiryAndDoesNotRaiseEvent()
    {
        var session = StartSession();
        session.ClearDomainEvents();
        var touchedAt = Now.AddMinutes(10);

        session.Touch(touchedAt);

        Assert.Equal(touchedAt, session.LastActivityAtUtc);
        Assert.Equal(touchedAt + IdleTimeout, session.ExpiresAtUtc);
        Assert.Empty(session.DomainEvents);
    }

    [Fact]
    public void Touch_WhenNotActive_Throws()
    {
        var session = StartSession();
        session.Revoke(Now);

        Assert.Throws<AuthenticationDomainException>(() => session.Touch(Now.AddMinutes(1)));
    }

    [Fact]
    public void Expire_WhenPastExpiry_Succeeds()
    {
        var session = StartSession();
        session.ClearDomainEvents();

        session.Expire(Now + IdleTimeout);

        Assert.Equal(SessionStatus.Expired, session.Status);
        Assert.IsType<SessionExpired>(Assert.Single(session.DomainEvents));
    }

    [Fact]
    public void Expire_BeforeExpiry_Throws()
    {
        var session = StartSession();

        Assert.Throws<AuthenticationDomainException>(() => session.Expire(Now.AddMinutes(1)));
    }

    [Fact]
    public void Expire_WhenNotActive_Throws()
    {
        var session = StartSession();
        session.Revoke(Now);

        Assert.Throws<AuthenticationDomainException>(() => session.Expire(Now + IdleTimeout));
    }

    [Fact]
    public void Revoke_WhenActive_Succeeds()
    {
        var session = StartSession();
        session.ClearDomainEvents();

        session.Revoke(Now);

        Assert.Equal(SessionStatus.Revoked, session.Status);
        Assert.IsType<SessionRevoked>(Assert.Single(session.DomainEvents));
    }

    [Fact]
    public void Revoke_WhenNotActive_Throws()
    {
        var session = StartSession();
        session.LogOut(Now);

        Assert.Throws<AuthenticationDomainException>(() => session.Revoke(Now));
    }

    [Fact]
    public void LogOut_WhenActive_Succeeds()
    {
        var session = StartSession();
        session.ClearDomainEvents();

        session.LogOut(Now);

        Assert.Equal(SessionStatus.LoggedOut, session.Status);
        Assert.IsType<SessionLoggedOut>(Assert.Single(session.DomainEvents));
    }

    [Fact]
    public void LogOut_WhenNotActive_Throws()
    {
        var session = StartSession();
        session.LogOut(Now);

        Assert.Throws<AuthenticationDomainException>(() => session.LogOut(Now));
    }
}
