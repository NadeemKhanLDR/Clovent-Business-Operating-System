using Clovent.Authentication.Application.Sessions.Queries;
using Clovent.Authentication.Application.Tests.TestSupport;
using Clovent.Authentication.Sessions;
using Clovent.Identity.Users;
using Xunit;

namespace Clovent.Authentication.Application.Tests.Sessions;

public class GetActiveSessionsForUserQueryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_ReturnsOnlyActiveSessionsForTheGivenUser()
    {
        var repository = new FakeSessionRepository();
        var userId = UserId.New();

        var active = Session.Start(userId, TimeSpan.FromMinutes(30), Now);
        var loggedOut = Session.Start(userId, TimeSpan.FromMinutes(30), Now);
        loggedOut.LogOut(Now);
        var otherUsersSession = Session.Start(UserId.New(), TimeSpan.FromMinutes(30), Now);

        await repository.AddAsync(active);
        await repository.AddAsync(loggedOut);
        await repository.AddAsync(otherUsersSession);

        var handler = new GetActiveSessionsForUserQueryHandler(repository);
        var result = await handler.Handle(new GetActiveSessionsForUserQuery(userId.Value), CancellationToken.None);

        var dto = Assert.Single(result);
        Assert.Equal(active.Id.Value, dto.SessionId);
    }
}
