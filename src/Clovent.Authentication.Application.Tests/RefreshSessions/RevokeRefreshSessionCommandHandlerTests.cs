using Clovent.Authentication.Application;
using Clovent.Authentication.Application.RefreshSessions.Commands;
using Clovent.Authentication.Application.Tests.TestSupport;
using Clovent.Authentication.RefreshSessions;
using Clovent.Authentication.Sessions;
using Xunit;

namespace Clovent.Authentication.Application.Tests.RefreshSessions;

public class RevokeRefreshSessionCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_ActiveRefreshSession_Revokes()
    {
        var refreshSessions = new FakeRefreshSessionRepository();
        var refreshSession = RefreshSession.Issue(SessionId.New(), TimeSpan.FromDays(7), Now);
        await refreshSessions.AddAsync(refreshSession);
        var handler = new RevokeRefreshSessionCommandHandler(refreshSessions, new FakeTimeProvider(Now));

        await handler.Handle(new RevokeRefreshSessionCommand(refreshSession.Id.Value), CancellationToken.None);

        Assert.Equal(RefreshSessionStatus.Revoked, refreshSession.Status);
    }

    [Fact]
    public async Task Handle_UnknownRefreshSession_ThrowsNotFound()
    {
        var handler = new RevokeRefreshSessionCommandHandler(new FakeRefreshSessionRepository(), new FakeTimeProvider(Now));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new RevokeRefreshSessionCommand(Guid.NewGuid()), CancellationToken.None));
    }
}
