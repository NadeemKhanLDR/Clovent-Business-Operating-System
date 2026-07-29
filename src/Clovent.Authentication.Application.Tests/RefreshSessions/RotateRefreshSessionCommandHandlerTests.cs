using Clovent.Authentication.Application;
using Clovent.Authentication.Application.RefreshSessions.Commands;
using Clovent.Authentication.Application.Tests.TestSupport;
using Clovent.Authentication.RefreshSessions;
using Clovent.Authentication.Sessions;
using Xunit;

namespace Clovent.Authentication.Application.Tests.RefreshSessions;

public class RotateRefreshSessionCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_ActiveRefreshSession_RotatesAndPersistsReplacement()
    {
        var refreshSessions = new FakeRefreshSessionRepository();
        var original = RefreshSession.Issue(SessionId.New(), TimeSpan.FromDays(7), Now);
        await refreshSessions.AddAsync(original);
        var handler = new RotateRefreshSessionCommandHandler(refreshSessions, new FakeTimeProvider(Now.AddDays(1)));

        var dto = await handler.Handle(new RotateRefreshSessionCommand(original.Id.Value), CancellationToken.None);

        Assert.NotEqual(original.Id.Value, dto.RefreshSessionId);
        Assert.Equal(RefreshSessionStatus.Rotated, original.Status);
        var stored = await refreshSessions.GetByIdAsync(new RefreshSessionId(dto.RefreshSessionId));
        Assert.NotNull(stored);
    }

    [Fact]
    public async Task Handle_UnknownRefreshSession_ThrowsNotFound()
    {
        var handler = new RotateRefreshSessionCommandHandler(new FakeRefreshSessionRepository(), new FakeTimeProvider(Now));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new RotateRefreshSessionCommand(Guid.NewGuid()), CancellationToken.None));
    }
}
