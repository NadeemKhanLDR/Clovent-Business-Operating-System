using Clovent.Authentication.Infrastructure.Repositories;
using Clovent.Authentication.Infrastructure.Tests.TestSupport;
using Clovent.Authentication.LoginAttempts;
using Clovent.Identity.Users;
using Xunit;

namespace Clovent.Authentication.Infrastructure.Tests.Repositories;

/// <summary>
/// Covers <see cref="LoginAttemptRepository"/>'s date-range filtering and
/// ordering - see <see cref="InMemoryTestBase"/> for why this uses the
/// InMemory provider rather than <see cref="SqliteTestBase"/>.
/// </summary>
public class LoginAttemptRepositoryFilteringTests : InMemoryTestBase
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetRecentByUserIdAsync_ReturnsOnlyAttemptsSinceCutoff_MostRecentFirst()
    {
        var userId = UserId.New();
        var old = LoginAttempt.Record("user@example.com", userId, LoginOutcome.InvalidCredentials, Now.AddMinutes(-30));
        var recent1 = LoginAttempt.Record("user@example.com", userId, LoginOutcome.InvalidCredentials, Now.AddMinutes(-5));
        var recent2 = LoginAttempt.Record("user@example.com", userId, LoginOutcome.InvalidCredentials, Now.AddMinutes(-1));

        await using (var writeContext = CreateContext())
        {
            var repository = new LoginAttemptRepository(writeContext);
            await repository.AddAsync(old);
            await repository.AddAsync(recent1);
            await repository.AddAsync(recent2);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var recent = await new LoginAttemptRepository(readContext)
            .GetRecentByUserIdAsync(userId, Now.AddMinutes(-10));

        Assert.Equal(2, recent.Count);
        Assert.Equal([recent2.Id, recent1.Id], recent.Select(a => a.Id));
    }

    [Fact]
    public async Task GetRecentByIdentifierAsync_FiltersByIdentifier()
    {
        var matching = LoginAttempt.Record("alice@example.com", null, LoginOutcome.UserNotFound, Now);
        var nonMatching = LoginAttempt.Record("bob@example.com", null, LoginOutcome.UserNotFound, Now);

        await using (var writeContext = CreateContext())
        {
            var repository = new LoginAttemptRepository(writeContext);
            await repository.AddAsync(matching);
            await repository.AddAsync(nonMatching);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var results = await new LoginAttemptRepository(readContext)
            .GetRecentByIdentifierAsync("alice@example.com", Now.AddMinutes(-1));

        Assert.Single(results);
        Assert.Equal(matching.Id, results.Single().Id);
    }
}
