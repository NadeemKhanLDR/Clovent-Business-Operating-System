using Clovent.Authentication.Application.Sessions;
using Clovent.Authentication.Application.Tests.TestSupport;
using Clovent.Authentication.RefreshSessions;
using Clovent.Authentication.Sessions;
using Xunit;

namespace Clovent.Authentication.Application.Tests.Sessions;

public class SessionTerminationCascadeTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ApplyAsync_ActiveRefreshSessionForSession_InvalidatesIt()
    {
        var refreshSessions = new FakeRefreshSessionRepository();
        var sessionId = SessionId.New();
        var refreshSession = RefreshSession.Issue(sessionId, TimeSpan.FromDays(7), Now);
        await refreshSessions.AddAsync(refreshSession);
        var cascade = new SessionTerminationCascade(refreshSessions);

        await cascade.ApplyAsync(sessionId, Now.AddMinutes(1), CancellationToken.None);

        Assert.Equal(RefreshSessionStatus.Revoked, refreshSession.Status);
    }

    [Fact]
    public async Task ApplyAsync_NoActiveRefreshSessionForSession_DoesNothing()
    {
        var cascade = new SessionTerminationCascade(new FakeRefreshSessionRepository());

        await cascade.ApplyAsync(SessionId.New(), Now, CancellationToken.None);
        // No exception is the assertion - there is nothing to invalidate.
    }

    [Fact]
    public async Task ApplyAsync_DoesNotAffectRefreshSessionsForOtherSessions()
    {
        var refreshSessions = new FakeRefreshSessionRepository();
        var unrelatedRefreshSession = RefreshSession.Issue(SessionId.New(), TimeSpan.FromDays(7), Now);
        await refreshSessions.AddAsync(unrelatedRefreshSession);
        var cascade = new SessionTerminationCascade(refreshSessions);

        await cascade.ApplyAsync(SessionId.New(), Now, CancellationToken.None);

        Assert.Equal(RefreshSessionStatus.Active, unrelatedRefreshSession.Status);
    }
}
