using Clovent.Authentication.Infrastructure.Repositories;
using Clovent.Authentication.Infrastructure.Tests.TestSupport;
using Clovent.Authentication.LoginAttempts;
using Xunit;

namespace Clovent.Authentication.Infrastructure.Tests.Repositories;

public class LoginAttemptRepositoryTests : SqliteTestBase
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsFields_IncludingNullUserId()
    {
        var attempt = LoginAttempt.Record("not-a-real-user", null, LoginOutcome.UserNotFound, Now);

        await using (var writeContext = CreateContext())
        {
            var repository = new LoginAttemptRepository(writeContext);
            await repository.AddAsync(attempt);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new LoginAttemptRepository(readContext).GetByIdAsync(attempt.Id);

        Assert.NotNull(reloaded);
        Assert.Equal("not-a-real-user", reloaded!.AttemptedIdentifier);
        Assert.Null(reloaded.UserId);
        Assert.Equal(LoginOutcome.UserNotFound, reloaded.Outcome);
    }
}
