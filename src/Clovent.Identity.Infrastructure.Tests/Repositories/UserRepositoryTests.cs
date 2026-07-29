using Clovent.Identity.Infrastructure.Repositories;
using Clovent.Identity.Infrastructure.Tests.TestSupport;
using Clovent.Identity.Roles;
using Clovent.Identity.Users;
using Clovent.Identity.Users.ValueObjects;
using Xunit;

namespace Clovent.Identity.Infrastructure.Tests.Repositories;

public class UserRepositoryTests : SqliteTestBase
{
    private static User CreateUser(string email = "alice@example.com", string userName = "alice") =>
        User.Create(Email.Create(email), UserName.Create(userName), DisplayName.Create("Alice Example"));

    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsFields()
    {
        var user = CreateUser();

        await using (var writeContext = CreateContext())
        {
            var repository = new UserRepository(writeContext);
            await repository.AddAsync(user);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new UserRepository(readContext).GetByIdAsync(user.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(user.Email, reloaded!.Email);
        Assert.Equal(user.UserName, reloaded.UserName);
        Assert.Equal(user.DisplayName, reloaded.DisplayName);
        Assert.Equal(UserStatus.PendingActivation, reloaded.Status);
    }

    [Fact]
    public async Task Activate_ThenReload_PersistsNewStatus()
    {
        var user = CreateUser();

        await using (var writeContext = CreateContext())
        {
            var repository = new UserRepository(writeContext);
            await repository.AddAsync(user);
            await writeContext.SaveChangesAsync();
        }

        await using (var updateContext = CreateContext())
        {
            var repository = new UserRepository(updateContext);
            var loaded = await repository.GetByIdAsync(user.Id);
            loaded!.Activate();
            await updateContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new UserRepository(readContext).GetByIdAsync(user.Id);

        Assert.Equal(UserStatus.Active, reloaded!.Status);
        // The CreatedAtUtc captured at Create() time must survive the
        // round trip unchanged - this is exactly the bug the constructor
        // fix in Milestone 9 prevents (see AuthenticationInfrastructure.md's
        // identical Session/RefreshSession reasoning).
        Assert.Equal(user.CreatedAtUtc, reloaded.CreatedAtUtc);
    }

    [Fact]
    public async Task GetByEmailAsync_FindsMatch()
    {
        var user = CreateUser(email: "bob@example.com", userName: "bob");

        await using (var writeContext = CreateContext())
        {
            var repository = new UserRepository(writeContext);
            await repository.AddAsync(user);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var found = await new UserRepository(readContext).GetByEmailAsync(Email.Create("bob@example.com"));

        Assert.NotNull(found);
        Assert.Equal(user.Id, found!.Id);
    }

    [Fact]
    public async Task GetByUserNameAsync_FindsMatch()
    {
        var user = CreateUser(email: "carol@example.com", userName: "carol");

        await using (var writeContext = CreateContext())
        {
            var repository = new UserRepository(writeContext);
            await repository.AddAsync(user);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var found = await new UserRepository(readContext).GetByUserNameAsync(UserName.Create("carol"));

        Assert.NotNull(found);
        Assert.Equal(user.Id, found!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NoMatch_ReturnsNull()
    {
        await using var context = CreateContext();
        var result = await new UserRepository(context).GetByIdAsync(UserId.New());

        Assert.Null(result);
    }

    [Fact]
    public async Task RoleIds_SurviveRoundTrip()
    {
        var user = CreateUser();
        var roleId1 = RoleId.New();
        var roleId2 = RoleId.New();
        user.AssignRole(roleId1);
        user.AssignRole(roleId2);

        await using (var writeContext = CreateContext())
        {
            var repository = new UserRepository(writeContext);
            await repository.AddAsync(user);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new UserRepository(readContext).GetByIdAsync(user.Id);

        Assert.Equal(2, reloaded!.RoleIds.Count);
        Assert.Contains(roleId1, reloaded.RoleIds);
        Assert.Contains(roleId2, reloaded.RoleIds);
    }

    [Fact]
    public async Task RemoveRole_ThenReload_PersistsRemoval()
    {
        var user = CreateUser();
        var roleId = RoleId.New();
        user.AssignRole(roleId);

        await using (var writeContext = CreateContext())
        {
            var repository = new UserRepository(writeContext);
            await repository.AddAsync(user);
            await writeContext.SaveChangesAsync();
        }

        await using (var updateContext = CreateContext())
        {
            var repository = new UserRepository(updateContext);
            var loaded = await repository.GetByIdAsync(user.Id);
            loaded!.RemoveRole(roleId);
            await updateContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new UserRepository(readContext).GetByIdAsync(user.Id);

        Assert.Empty(reloaded!.RoleIds);
    }
}
