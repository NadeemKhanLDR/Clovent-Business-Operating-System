using Clovent.Authentication.Application.LoginAttempts.Queries;
using Clovent.Authentication.Application.Tests.TestSupport;
using Clovent.Authentication.LoginAttempts;
using Xunit;

namespace Clovent.Authentication.Application.Tests.LoginAttempts;

public class GetRecentLoginAttemptsQueryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_ExcludesAttemptsOutsideTheWindow()
    {
        var repository = new FakeLoginAttemptRepository();
        var withinWindow = LoginAttempt.Record("ada@example.com", null, LoginOutcome.InvalidCredentials, Now.AddMinutes(-5));
        var outsideWindow = LoginAttempt.Record("ada@example.com", null, LoginOutcome.InvalidCredentials, Now.AddHours(-2));
        await repository.AddAsync(withinWindow);
        await repository.AddAsync(outsideWindow);

        var handler = new GetRecentLoginAttemptsQueryHandler(repository, new FakeTimeProvider(Now));
        var result = await handler.Handle(
            new GetRecentLoginAttemptsQuery("ada@example.com", TimeSpan.FromMinutes(15)),
            CancellationToken.None);

        var dto = Assert.Single(result);
        Assert.Equal(withinWindow.Id.Value, dto.LoginAttemptId);
    }
}
