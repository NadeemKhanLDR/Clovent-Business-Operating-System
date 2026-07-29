using Clovent.Authentication.Application;
using Clovent.Authentication.Application.RefreshSessions.Commands;
using Clovent.Authentication.Application.Tests.TestSupport;
using Clovent.Authentication.Sessions;
using Clovent.Identity.Users;
using Xunit;

namespace Clovent.Authentication.Application.Tests.RefreshSessions;

public class IssueRefreshSessionCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_ExistingSession_IssuesAndPersistsRefreshSession()
    {
        var sessions = new FakeSessionRepository();
        var refreshSessions = new FakeRefreshSessionRepository();
        var session = Session.Start(UserId.New(), TimeSpan.FromMinutes(30), Now);
        await sessions.AddAsync(session);
        var handler = new IssueRefreshSessionCommandHandler(refreshSessions, sessions, new FakeTimeProvider(Now));

        var dto = await handler.Handle(new IssueRefreshSessionCommand(session.Id.Value), CancellationToken.None);

        Assert.Equal(session.Id.Value, dto.SessionId);
        Assert.Equal("Active", dto.Status);
        var stored = await refreshSessions.GetByIdAsync(new Clovent.Authentication.RefreshSessions.RefreshSessionId(dto.RefreshSessionId));
        Assert.NotNull(stored);
    }

    [Fact]
    public async Task Handle_UnknownSession_ThrowsNotFound()
    {
        var handler = new IssueRefreshSessionCommandHandler(
            new FakeRefreshSessionRepository(), new FakeSessionRepository(), new FakeTimeProvider(Now));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new IssueRefreshSessionCommand(Guid.NewGuid()), CancellationToken.None));
    }
}
