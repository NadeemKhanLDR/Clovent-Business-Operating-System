using Clovent.Identity.Infrastructure.Repositories;
using Clovent.Identity.Infrastructure.Tests.TestSupport;
using Clovent.Identity.Permissions;
using Clovent.Identity.Roles;
using Clovent.Identity.Roles.ValueObjects;
using Xunit;

namespace Clovent.Identity.Infrastructure.Tests.Repositories;

public class RoleRepositoryTests : SqliteTestBase
{
    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsFields()
    {
        var role = Role.Create(RoleName.Create("Branch Manager"));

        await using (var writeContext = CreateContext())
        {
            var repository = new RoleRepository(writeContext);
            await repository.AddAsync(role);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new RoleRepository(readContext).GetByIdAsync(role.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(role.Name, reloaded!.Name);
        Assert.Empty(reloaded.PermissionIds);
    }

    [Fact]
    public async Task PermissionIds_SurviveRoundTrip()
    {
        var role = Role.Create(RoleName.Create("Auditor"));
        var permissionId1 = PermissionId.New();
        var permissionId2 = PermissionId.New();
        role.AddPermission(permissionId1);
        role.AddPermission(permissionId2);

        await using (var writeContext = CreateContext())
        {
            var repository = new RoleRepository(writeContext);
            await repository.AddAsync(role);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new RoleRepository(readContext).GetByIdAsync(role.Id);

        Assert.Equal(2, reloaded!.PermissionIds.Count);
        Assert.Contains(permissionId1, reloaded.PermissionIds);
        Assert.Contains(permissionId2, reloaded.PermissionIds);
    }

    [Fact]
    public async Task GetByNameAsync_FindsMatch()
    {
        var role = Role.Create(RoleName.Create("Cashier"));

        await using (var writeContext = CreateContext())
        {
            var repository = new RoleRepository(writeContext);
            await repository.AddAsync(role);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var found = await new RoleRepository(readContext).GetByNameAsync(RoleName.Create("Cashier"));

        Assert.NotNull(found);
        Assert.Equal(role.Id, found!.Id);
    }
}
