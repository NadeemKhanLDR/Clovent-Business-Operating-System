using Clovent.Authentication.Application;
using Clovent.Authentication.Application.Sessions;
using Clovent.Authentication.Application.Sessions.Commands;
using Clovent.Authentication.Application.Tests.TestSupport;
using Clovent.Authentication.RefreshSessions;
using Clovent.Authentication.Sessions;
using Clovent.Identity.Users;
using Xunit;

namespace Clovent.Authentication.Application.Tests.Sessions;

public class LogOutSessionCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_ExistingActiveSession_LogsOut()
    {
        var sessions = new FakeSessionRepository();
        var session = Session.Start(UserId.New(), TimeSpan.FromMinutes(30), Now);
        await sessions.AddAsync(session);
        var cascade = new SessionTerminationCascade(new FakeRefreshSessionRepository());
        var handler = new LogOutSessionCommandHandler(sessions, cascade, new FakeTimeProvider(Now));

        await handler.Handle(new LogOutSessionCommand(session.Id.Value), CancellationToken.None);

        Assert.Equal(SessionStatus.LoggedOut, session.Status);
    }

    [Fact]
    public async Task Handle_UnknownSession_ThrowsNotFound()
    {
        var cascade = new SessionTerminationCascade(new FakeRefreshSessionRepository());
        var handler = new LogOutSessionCommandHandler(new FakeSessionRepository(), cascade, new FakeTimeProvider(Now));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new LogOutSessionCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SessionHasActiveRefreshSession_CascadesInvalidation()
    {
        var sessions = new FakeSessionRepository();
        var refreshSessions = new FakeRefreshSessionRepository();
        var session = Session.Start(UserId.New(), TimeSpan.FromMinutes(30), Now);
        await sessions.AddAsync(session);
        var refreshSession = RefreshSession.Issue(session.Id, TimeSpan.FromDays(7), Now);
        await refreshSessions.AddAsync(refreshSession);
        var cascade = new SessionTerminationCascade(refreshSessions);
        var handler = new LogOutSessionCommandHandler(sessions, cascade, new FakeTimeProvider(Now));

        await handler.Handle(new LogOutSessionCommand(session.Id.Value), CancellationToken.None);

        Assert.Equal(RefreshSessionStatus.Revoked, refreshSession.Status);
    }
}
