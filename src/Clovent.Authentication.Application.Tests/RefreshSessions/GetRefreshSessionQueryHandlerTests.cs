using Clovent.Authentication.Application.RefreshSessions.Queries;
using Clovent.Authentication.Application.Tests.TestSupport;
using Clovent.Authentication.RefreshSessions;
using Clovent.Authentication.Sessions;
using Xunit;

namespace Clovent.Authentication.Application.Tests.RefreshSessions;

public class GetRefreshSessionQueryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_ExistingRefreshSession_ReturnsDto()
    {
        var repository = new FakeRefreshSessionRepository();
        var refreshSession = RefreshSession.Issue(SessionId.New(), TimeSpan.FromDays(7), Now);
        await repository.AddAsync(refreshSession);
        var handler = new GetRefreshSessionQueryHandler(repository);

        var dto = await handler.Handle(new GetRefreshSessionQuery(refreshSession.Id.Value), CancellationToken.None);

        Assert.NotNull(dto);
        Assert.Equal(refreshSession.Id.Value, dto!.RefreshSessionId);
    }

    [Fact]
    public async Task Handle_UnknownRefreshSession_ReturnsNull()
    {
        var handler = new GetRefreshSessionQueryHandler(new FakeRefreshSessionRepository());

        var dto = await handler.Handle(new GetRefreshSessionQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(dto);
    }
}
