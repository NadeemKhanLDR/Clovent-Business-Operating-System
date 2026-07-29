using Clovent.Authentication;
using Clovent.Authentication.RefreshSessions;
using Clovent.Authentication.RefreshSessions.Events;
using Clovent.Authentication.Sessions;
using Xunit;

namespace Clovent.Authentication.Tests.RefreshSessions;

public class RefreshSessionTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    private static RefreshSession IssueRefreshSession() => RefreshSession.Issue(SessionId.New(), Lifetime, Now);

    [Fact]
    public void Issue_SetsActiveAndRaisesRefreshSessionIssued()
    {
        var refreshSession = IssueRefreshSession();

        Assert.Equal(RefreshSessionStatus.Active, refreshSession.Status);
        Assert.Equal(Now + Lifetime, refreshSession.ExpiresAtUtc);
        Assert.IsType<RefreshSessionIssued>(Assert.Single(refreshSession.DomainEvents));
    }

    [Fact]
    public void Issue_NonPositiveLifetime_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RefreshSession.Issue(SessionId.New(), TimeSpan.Zero, Now));
    }

    [Fact]
    public void Rotate_WhenActive_MarksRotatedAndReturnsNewActiveSession()
    {
        var original = IssueRefreshSession();
        original.ClearDomainEvents();
        var rotateAt = Now.AddDays(1);

        var replacement = original.Rotate(Lifetime, rotateAt);

        Assert.Equal(RefreshSessionStatus.Rotated, original.Status);
        Assert.Equal(RefreshSessionStatus.Active, replacement.Status);
        Assert.Equal(original.SessionId, replacement.SessionId);
        Assert.NotEqual(original.Id, replacement.Id);
        Assert.IsType<RefreshSessionRotated>(Assert.Single(original.DomainEvents));
        Assert.IsType<RefreshSessionIssued>(Assert.Single(replacement.DomainEvents));
    }

    [Fact]
    public void Rotate_WhenNotActive_Throws()
    {
        var refreshSession = IssueRefreshSession();
        refreshSession.Revoke(Now);

        Assert.Throws<AuthenticationDomainException>(() => refreshSession.Rotate(Lifetime, Now.AddDays(1)));
    }

    [Fact]
    public void Rotate_WhenExpired_Throws()
    {
        var refreshSession = IssueRefreshSession();

        Assert.Throws<AuthenticationDomainException>(() => refreshSession.Rotate(Lifetime, Now + Lifetime));
    }

    [Fact]
    public void Revoke_WhenActive_Succeeds()
    {
        var refreshSession = IssueRefreshSession();
        refreshSession.ClearDomainEvents();

        refreshSession.Revoke(Now);

        Assert.Equal(RefreshSessionStatus.Revoked, refreshSession.Status);
        Assert.IsType<RefreshSessionRevoked>(Assert.Single(refreshSession.DomainEvents));
    }

    [Fact]
    public void Revoke_WhenNotActive_Throws()
    {
        var refreshSession = IssueRefreshSession();
        refreshSession.Revoke(Now);

        Assert.Throws<AuthenticationDomainException>(() => refreshSession.Revoke(Now));
    }

    [Fact]
    public void Expire_WhenPastExpiry_Succeeds()
    {
        var refreshSession = IssueRefreshSession();
        refreshSession.ClearDomainEvents();

        refreshSession.Expire(Now + Lifetime);

        Assert.Equal(RefreshSessionStatus.Expired, refreshSession.Status);
        Assert.IsType<RefreshSessionExpired>(Assert.Single(refreshSession.DomainEvents));
    }

    [Fact]
    public void Expire_BeforeExpiry_Throws()
    {
        var refreshSession = IssueRefreshSession();

        Assert.Throws<AuthenticationDomainException>(() => refreshSession.Expire(Now.AddDays(1)));
    }

    [Fact]
    public void Expire_WhenNotActive_Throws()
    {
        var refreshSession = IssueRefreshSession();
        refreshSession.Revoke(Now);

        Assert.Throws<AuthenticationDomainException>(() => refreshSession.Expire(Now + Lifetime));
    }

    [Fact]
    public void Invalidate_WhenActive_TransitionsToRevokedAndRaisesEvent()
    {
        var refreshSession = IssueRefreshSession();
        refreshSession.ClearDomainEvents();

        refreshSession.Invalidate(Now.AddHours(1));

        Assert.Equal(RefreshSessionStatus.Revoked, refreshSession.Status);
        Assert.IsType<RefreshSessionRevoked>(Assert.Single(refreshSession.DomainEvents));
    }

    [Theory]
    [MemberData(nameof(TerminalStateSetups))]
    public void Invalidate_WhenAlreadyTerminal_IsNoOpAndDoesNotThrow(Action<RefreshSession> putInTerminalState, RefreshSessionStatus expectedStatus)
    {
        var refreshSession = IssueRefreshSession();
        putInTerminalState(refreshSession);
        refreshSession.ClearDomainEvents();

        var exception = Record.Exception(() => refreshSession.Invalidate(Now.AddDays(1)));

        Assert.Null(exception);
        Assert.Equal(expectedStatus, refreshSession.Status);
        Assert.Empty(refreshSession.DomainEvents);
    }

    public static IEnumerable<object[]> TerminalStateSetups()
    {
        yield return
        [
            (Action<RefreshSession>)(r => r.Revoke(Now)),
            RefreshSessionStatus.Revoked
        ];
        yield return
        [
            (Action<RefreshSession>)(r => r.Expire(Now + Lifetime)),
            RefreshSessionStatus.Expired
        ];
        yield return
        [
            (Action<RefreshSession>)(r => r.Rotate(Lifetime, Now)),
            RefreshSessionStatus.Rotated
        ];
    }
}
