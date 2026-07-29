using Clovent.Authentication.Infrastructure.Repositories;
using Clovent.Authentication.Infrastructure.Tests.TestSupport;
using Clovent.Authentication.Sessions;
using Clovent.Identity.Users;
using Xunit;

namespace Clovent.Authentication.Infrastructure.Tests.Repositories;

public class SessionRepositoryTests : SqliteTestBase
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsAllFields()
    {
        var userId = UserId.New();
        var session = Session.Start(userId, TimeSpan.FromMinutes(30), Now, Shared.ValueObjects.IpAddress.Create("203.0.113.7"));

        await using (var writeContext = CreateContext())
        {
            var repository = new SessionRepository(writeContext);
            await repository.AddAsync(session);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new SessionRepository(readContext).GetByIdAsync(session.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(session.Id, reloaded!.Id);
        Assert.Equal(userId, reloaded.UserId);
        Assert.Equal("203.0.113.7", reloaded.IpAddress!.Value);
        Assert.Equal(TimeSpan.FromMinutes(30), reloaded.IdleTimeout);
        Assert.Equal(SessionStatus.Active, reloaded.Status);
    }

    [Fact]
    public async Task Revoke_ThenReload_PersistsNewStatus()
    {
        var session = Session.Start(UserId.New(), TimeSpan.FromMinutes(30), Now);

        await using (var writeContext = CreateContext())
        {
            var repository = new SessionRepository(writeContext);
            await repository.AddAsync(session);
            await writeContext.SaveChangesAsync();
        }

        await using (var updateContext = CreateContext())
        {
            var repository = new SessionRepository(updateContext);
            var loaded = await repository.GetByIdAsync(session.Id);
            loaded!.Revoke(Now.AddMinutes(5));
            await updateContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new SessionRepository(readContext).GetByIdAsync(session.Id);

        Assert.Equal(SessionStatus.Revoked, reloaded!.Status);
    }

    [Fact]
    public async Task GetActiveByUserIdAsync_OnlyReturnsActiveSessionsForThatUser()
    {
        var userId = UserId.New();
        var otherUserId = UserId.New();

        var activeSession = Session.Start(userId, TimeSpan.FromMinutes(30), Now);
        var revokedSession = Session.Start(userId, TimeSpan.FromMinutes(30), Now);
        revokedSession.Revoke(Now.AddMinutes(1));
        var otherUsersSession = Session.Start(otherUserId, TimeSpan.FromMinutes(30), Now);

        await using (var writeContext = CreateContext())
        {
            var repository = new SessionRepository(writeContext);
            await repository.AddAsync(activeSession);
            await repository.AddAsync(revokedSession);
            await repository.AddAsync(otherUsersSession);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var active = await new SessionRepository(readContext).GetActiveByUserIdAsync(userId);

        Assert.Single(active);
        Assert.Equal(activeSession.Id, active.Single().Id);
    }
}
