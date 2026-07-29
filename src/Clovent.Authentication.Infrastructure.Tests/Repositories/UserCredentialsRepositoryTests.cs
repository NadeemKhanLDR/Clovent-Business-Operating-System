using Clovent.Authentication.Credentials;
using Clovent.Authentication.Infrastructure.Repositories;
using Clovent.Authentication.Infrastructure.Tests.TestSupport;
using Clovent.Identity.Users;
using Xunit;

namespace Clovent.Authentication.Infrastructure.Tests.Repositories;

public class UserCredentialsRepositoryTests : SqliteTestBase
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AddAsync_ThenGetByUserId_RoundTripsFields()
    {
        var userId = UserId.New();
        var credentials = UserCredentials.Create(userId, Now);
        credentials.SetPassword(PasswordHash.Create("hash-1"), Now);
        credentials.RecordFailedAttempt();
        credentials.RecordFailedAttempt();

        await using (var writeContext = CreateContext())
        {
            var repository = new UserCredentialsRepository(writeContext);
            await repository.AddAsync(credentials);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new UserCredentialsRepository(readContext).GetByUserIdAsync(userId);

        Assert.NotNull(reloaded);
        Assert.Equal(userId, reloaded!.UserId);
        Assert.Equal(PasswordHash.Create("hash-1"), reloaded.PasswordHash);
        Assert.Equal(2, reloaded.FailedAttempts.Count);
        Assert.Equal(credentials.SecurityStamp, reloaded.SecurityStamp);
    }

    [Fact]
    public async Task PasswordHistory_SurvivesMultipleChangesAndOrderingViaJsonRoundTrip()
    {
        var userId = UserId.New();
        var credentials = UserCredentials.Create(userId, Now);
        credentials.SetPassword(PasswordHash.Create("hash-1"), Now);
        credentials.SetPassword(PasswordHash.Create("hash-2"), Now.AddDays(1));
        credentials.SetPassword(PasswordHash.Create("hash-3"), Now.AddDays(2));

        await using (var writeContext = CreateContext())
        {
            var repository = new UserCredentialsRepository(writeContext);
            await repository.AddAsync(credentials);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new UserCredentialsRepository(readContext).GetByUserIdAsync(userId);

        Assert.Equal(3, reloaded!.PasswordHistory.Entries.Count);
        Assert.Equal(PasswordHash.Create("hash-3"), reloaded.PasswordHistory.Entries[0].Hash);
        Assert.Equal(PasswordHash.Create("hash-2"), reloaded.PasswordHistory.Entries[1].Hash);
        Assert.Equal(PasswordHash.Create("hash-1"), reloaded.PasswordHistory.Entries[2].Hash);
        Assert.Equal(Now.AddDays(2), reloaded.PasswordHistory.LastChangedAtUtc);
        Assert.True(reloaded.PasswordHistory.Contains(PasswordHash.Create("hash-1")));
    }

    [Fact]
    public async Task GetByUserIdAsync_NoMatch_ReturnsNull()
    {
        await using var context = CreateContext();
        var result = await new UserCredentialsRepository(context).GetByUserIdAsync(UserId.New());

        Assert.Null(result);
    }
}
