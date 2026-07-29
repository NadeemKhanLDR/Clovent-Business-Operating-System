using Clovent.Authentication.Infrastructure.Repositories;
using Clovent.Authentication.Infrastructure.Tests.TestSupport;
using Clovent.Authentication.RefreshSessions;
using Clovent.Authentication.Sessions;
using Xunit;

namespace Clovent.Authentication.Infrastructure.Tests.Repositories;

public class RefreshSessionRepositoryTests : SqliteTestBase
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsFields()
    {
        var sessionId = SessionId.New();
        var refreshSession = RefreshSession.Issue(sessionId, TimeSpan.FromDays(7), Now);

        await using (var writeContext = CreateContext())
        {
            var repository = new RefreshSessionRepository(writeContext);
            await repository.AddAsync(refreshSession);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new RefreshSessionRepository(readContext).GetByIdAsync(refreshSession.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(sessionId, reloaded!.SessionId);
        Assert.Equal(RefreshSessionStatus.Active, reloaded.Status);
        Assert.Equal(Now + TimeSpan.FromDays(7), reloaded.ExpiresAtUtc);
    }

    [Fact]
    public async Task GetActiveBySessionIdAsync_IgnoresRotatedPredecessor()
    {
        var sessionId = SessionId.New();
        var original = RefreshSession.Issue(sessionId, TimeSpan.FromDays(7), Now);
        var replacement = original.Rotate(TimeSpan.FromDays(7), Now.AddHours(1));

        await using (var writeContext = CreateContext())
        {
            var repository = new RefreshSessionRepository(writeContext);
            await repository.AddAsync(original);
            await repository.AddAsync(replacement);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var active = await new RefreshSessionRepository(readContext).GetActiveBySessionIdAsync(sessionId);

        Assert.NotNull(active);
        Assert.Equal(replacement.Id, active!.Id);
    }

    [Fact]
    public async Task Invalidate_ThenReload_PersistsRevokedStatus()
    {
        var refreshSession = RefreshSession.Issue(SessionId.New(), TimeSpan.FromDays(7), Now);

        await using (var writeContext = CreateContext())
        {
            var repository = new RefreshSessionRepository(writeContext);
            await repository.AddAsync(refreshSession);
            await writeContext.SaveChangesAsync();
        }

        await using (var updateContext = CreateContext())
        {
            var repository = new RefreshSessionRepository(updateContext);
            var loaded = await repository.GetByIdAsync(refreshSession.Id);
            loaded!.Invalidate(Now.AddHours(1));
            await updateContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new RefreshSessionRepository(readContext).GetByIdAsync(refreshSession.Id);

        Assert.Equal(RefreshSessionStatus.Revoked, reloaded!.Status);
    }
}
