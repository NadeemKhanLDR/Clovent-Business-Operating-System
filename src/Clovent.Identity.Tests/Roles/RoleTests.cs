using Clovent.Identity;
using Clovent.Identity.Permissions;
using Clovent.Identity.Roles;
using Clovent.Identity.Roles.Events;
using Clovent.Identity.Roles.ValueObjects;
using Xunit;

namespace Clovent.Identity.Tests.Roles;

public class RoleTests
{
    [Fact]
    public void Create_RaisesRoleCreated()
    {
        var role = Role.Create(RoleName.Create("Manager"));

        Assert.Empty(role.PermissionIds);
        Assert.IsType<RoleCreated>(Assert.Single(role.DomainEvents));
    }

    [Fact]
    public void Rename_DifferentValue_UpdatesAndRaisesEvent()
    {
        var role = Role.Create(RoleName.Create("Manager"));
        role.ClearDomainEvents();

        role.Rename(RoleName.Create("Senior Manager"));

        Assert.Equal(RoleName.Create("Senior Manager"), role.Name);
        Assert.IsType<RoleRenamed>(Assert.Single(role.DomainEvents));
    }

    [Fact]
    public void Rename_SameValue_IsNoOp()
    {
        var role = Role.Create(RoleName.Create("Manager"));
        role.ClearDomainEvents();

        role.Rename(RoleName.Create("Manager"));

        Assert.Empty(role.DomainEvents);
    }

    [Fact]
    public void AddPermission_New_Succeeds()
    {
        var role = Role.Create(RoleName.Create("Manager"));
        var permissionId = PermissionId.New();

        role.AddPermission(permissionId);

        Assert.Contains(permissionId, role.PermissionIds);
        Assert.IsType<PermissionAssignedToRole>(role.DomainEvents.Last());
    }

    [Fact]
    public void AddPermission_AlreadyGranted_Throws()
    {
        var role = Role.Create(RoleName.Create("Manager"));
        var permissionId = PermissionId.New();
        role.AddPermission(permissionId);

        Assert.Throws<IdentityDomainException>(() => role.AddPermission(permissionId));
    }

    [Fact]
    public void RemovePermission_Granted_Succeeds()
    {
        var role = Role.Create(RoleName.Create("Manager"));
        var permissionId = PermissionId.New();
        role.AddPermission(permissionId);

        role.RemovePermission(permissionId);

        Assert.DoesNotContain(permissionId, role.PermissionIds);
        Assert.IsType<PermissionRemovedFromRole>(role.DomainEvents.Last());
    }

    [Fact]
    public void RemovePermission_NotGranted_Throws()
    {
        var role = Role.Create(RoleName.Create("Manager"));

        Assert.Throws<IdentityDomainException>(() => role.RemovePermission(PermissionId.New()));
    }
}
