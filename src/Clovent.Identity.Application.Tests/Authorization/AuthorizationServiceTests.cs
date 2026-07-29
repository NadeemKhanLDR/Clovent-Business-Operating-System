using Clovent.Identity.Application.Authorization;
using Clovent.Identity.Application.Tests.TestSupport;
using Clovent.Identity.Permissions;
using Clovent.Identity.Permissions.ValueObjects;
using Clovent.Identity.Roles;
using Clovent.Identity.Roles.ValueObjects;
using Clovent.Identity.Users;
using Clovent.Identity.Users.ValueObjects;
using Xunit;

namespace Clovent.Identity.Application.Tests.Authorization;

public class AuthorizationServiceTests
{
    private sealed class Fixture
    {
        public FakeUserRepository Users { get; } = new();
        public FakeRoleRepository Roles { get; } = new();
        public FakePermissionRepository Permissions { get; } = new();
        public FakePermissionCache Cache { get; } = new();
        public AuthorizationPolicyProvider PolicyProvider { get; } = new();

        public AuthorizationService BuildService() =>
            new(Users, Roles, Permissions, Cache, PolicyProvider);
    }

    private static (User user, Role role, Permission permission) SetUpUserWithPermission(Fixture fixture, string permissionCode)
    {
        var user = User.Create(Email.Create("alice@example.com"), UserName.Create("alice"), DisplayName.Create("Alice"));
        var permission = Permission.Create(PermissionCode.Create(permissionCode), "test permission");
        var role = Role.Create(RoleName.Create("Manager"));
        role.AddPermission(permission.Id);
        user.AssignRole(role.Id);

        fixture.Users.Add(user);
        fixture.Roles.Add(role);
        fixture.Permissions.Add(permission);

        return (user, role, permission);
    }

    [Fact]
    public async Task GetPermissionCodesAsync_ResolvesThroughAssignedRoles()
    {
        var fixture = new Fixture();
        var (user, _, permission) = SetUpUserWithPermission(fixture, "module.restaurantpos");
        var service = fixture.BuildService();

        var codes = await service.GetPermissionCodesAsync(user.Id.Value);

        Assert.Contains(permission.Code.Value, codes);
    }

    [Fact]
    public async Task GetPermissionCodesAsync_UnknownUser_ReturnsEmpty()
    {
        var fixture = new Fixture();
        var service = fixture.BuildService();

        var codes = await service.GetPermissionCodesAsync(Guid.NewGuid());

        Assert.Empty(codes);
    }

    [Fact]
    public async Task GetPermissionCodesAsync_CachesResultAfterFirstResolution()
    {
        var fixture = new Fixture();
        var (user, _, _) = SetUpUserWithPermission(fixture, "module.restaurantpos");
        var service = fixture.BuildService();

        await service.GetPermissionCodesAsync(user.Id.Value);

        Assert.Equal(1, fixture.Cache.SetCallCount);
    }

    [Fact]
    public async Task GetPermissionCodesAsync_WhenCached_DoesNotResolveAgain()
    {
        var fixture = new Fixture();
        var (user, _, permission) = SetUpUserWithPermission(fixture, "module.restaurantpos");
        await fixture.Cache.SetAsync(user.Id.Value, [permission.Code.Value]);
        var service = fixture.BuildService();

        var codes = await service.GetPermissionCodesAsync(user.Id.Value);

        // The one SetCallCount is from seeding the cache directly above -
        // the service itself must not call Set again on a cache hit.
        Assert.Equal(1, fixture.Cache.SetCallCount);
        Assert.Contains(permission.Code.Value, codes);
    }

    [Fact]
    public async Task HasPermissionAsync_GrantedPermission_ReturnsTrue()
    {
        var fixture = new Fixture();
        var (user, _, _) = SetUpUserWithPermission(fixture, "module.restaurantpos");
        var service = fixture.BuildService();

        Assert.True(await service.HasPermissionAsync(user.Id.Value, "module.restaurantpos"));
    }

    [Fact]
    public async Task HasPermissionAsync_UngrantedPermission_ReturnsFalse()
    {
        var fixture = new Fixture();
        var (user, _, _) = SetUpUserWithPermission(fixture, "module.restaurantpos");
        var service = fixture.BuildService();

        Assert.False(await service.HasPermissionAsync(user.Id.Value, "module.inventory"));
    }

    [Fact]
    public async Task HasRoleAsync_AssignedRole_ReturnsTrue()
    {
        var fixture = new Fixture();
        var (user, _, _) = SetUpUserWithPermission(fixture, "module.restaurantpos");
        var service = fixture.BuildService();

        Assert.True(await service.HasRoleAsync(user.Id.Value, "Manager"));
    }

    [Fact]
    public async Task HasRoleAsync_UnassignedRole_ReturnsFalse()
    {
        var fixture = new Fixture();
        var (user, _, _) = SetUpUserWithPermission(fixture, "module.restaurantpos");
        var service = fixture.BuildService();

        Assert.False(await service.HasRoleAsync(user.Id.Value, "Cashier"));
    }

    [Fact]
    public async Task SatisfiesPolicyAsync_UserHoldsAllRequiredCodes_ReturnsTrue()
    {
        var fixture = new Fixture();
        var (user, role, _) = SetUpUserWithPermission(fixture, "module.restaurantpos");
        var secondPermission = Permission.Create(PermissionCode.Create("feature.export.excel"), "export");
        role.AddPermission(secondPermission.Id);
        fixture.Permissions.Add(secondPermission);
        fixture.PolicyProvider.AddPolicy(new AuthorizationPolicy("CanRunReports", ["module.restaurantpos", "feature.export.excel"]));
        var service = fixture.BuildService();

        Assert.True(await service.SatisfiesPolicyAsync(user.Id.Value, "CanRunReports"));
    }

    [Fact]
    public async Task SatisfiesPolicyAsync_MissingOneRequiredCode_ReturnsFalse()
    {
        var fixture = new Fixture();
        var (user, _, _) = SetUpUserWithPermission(fixture, "module.restaurantpos");
        fixture.PolicyProvider.AddPolicy(new AuthorizationPolicy("CanRunReports", ["module.restaurantpos", "feature.export.excel"]));
        var service = fixture.BuildService();

        Assert.False(await service.SatisfiesPolicyAsync(user.Id.Value, "CanRunReports"));
    }

    [Fact]
    public async Task SatisfiesPolicyAsync_UnregisteredPolicy_ReturnsFalse()
    {
        var fixture = new Fixture();
        var (user, _, _) = SetUpUserWithPermission(fixture, "module.restaurantpos");
        var service = fixture.BuildService();

        Assert.False(await service.SatisfiesPolicyAsync(user.Id.Value, "NoSuchPolicy"));
    }
}
