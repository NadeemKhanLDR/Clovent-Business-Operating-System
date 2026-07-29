using Clovent.Authentication.Infrastructure.Persistence;
using Clovent.Authentication.Infrastructure.Repositories;
using Clovent.Authentication.Infrastructure.Tests.TestSupport;
using Clovent.Authentication.Sessions;
using Clovent.Identity.Users;
using Xunit;

namespace Clovent.Authentication.Infrastructure.Tests.Persistence;

public class UnitOfWorkTests : SqliteTestBase
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SaveChangesAsync_PersistsChangesTrackedByTheContext()
    {
        var session = Session.Start(UserId.New(), TimeSpan.FromMinutes(30), Now);

        await using var context = CreateContext();
        var repository = new SessionRepository(context);
        var unitOfWork = new UnitOfWork(context);

        await repository.AddAsync(session);
        await unitOfWork.SaveChangesAsync();

        await using var readContext = CreateContext();
        var reloaded = await new SessionRepository(readContext).GetByIdAsync(session.Id);

        Assert.NotNull(reloaded);
    }
}
