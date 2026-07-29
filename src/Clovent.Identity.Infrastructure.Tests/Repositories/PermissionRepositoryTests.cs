using Clovent.Identity.Infrastructure.Repositories;
using Clovent.Identity.Infrastructure.Tests.TestSupport;
using Clovent.Identity.Permissions;
using Clovent.Identity.Permissions.ValueObjects;
using Xunit;

namespace Clovent.Identity.Infrastructure.Tests.Repositories;

public class PermissionRepositoryTests : SqliteTestBase
{
    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsFields()
    {
        var permission = Permission.Create(PermissionCode.Create("identity.users.manage"), "Manage users");

        await using (var writeContext = CreateContext())
        {
            var repository = new PermissionRepository(writeContext);
            await repository.AddAsync(permission);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new PermissionRepository(readContext).GetByIdAsync(permission.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(permission.Code, reloaded!.Code);
        Assert.Equal("Manage users", reloaded.Description);
    }

    [Fact]
    public async Task GetByCodeAsync_FindsMatch()
    {
        var permission = Permission.Create(PermissionCode.Create("module.restaurantpos"), "Access Restaurant POS module");

        await using (var writeContext = CreateContext())
        {
            var repository = new PermissionRepository(writeContext);
            await repository.AddAsync(permission);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var found = await new PermissionRepository(readContext).GetByCodeAsync(PermissionCode.Create("module.restaurantpos"));

        Assert.NotNull(found);
        Assert.Equal(permission.Id, found!.Id);
    }

    [Fact]
    public async Task GetByCodeAsync_NoMatch_ReturnsNull()
    {
        await using var context = CreateContext();
        var result = await new PermissionRepository(context).GetByCodeAsync(PermissionCode.Create("nothing.here"));

        Assert.Null(result);
    }
}
