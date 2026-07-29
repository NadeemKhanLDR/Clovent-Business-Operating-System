using Clovent.Domain;
using Clovent.Identity.Permissions;
using Clovent.Identity.Roles.Events;
using Clovent.Identity.Roles.ValueObjects;

namespace Clovent.Identity.Roles;

/// <summary>
/// A named collection of <see cref="Permission"/>s that can be assigned to
/// <see cref="Users.User"/>s. References permissions by identity only - a
/// Role does not own the permission's lifecycle.
/// </summary>
public sealed class Role : AggregateRoot<RoleId>
{
    private readonly HashSet<PermissionId> _permissionIds;

    /// <summary>The role's current name.</summary>
    public RoleName Name { get; private set; }

    /// <summary>The permissions currently granted by this role.</summary>
    public IReadOnlyCollection<PermissionId> PermissionIds => _permissionIds;

    /// <summary>UTC instant this role was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>
    /// Takes <paramref name="createdAtUtc"/> and <paramref name="permissionIds"/>
    /// explicitly (rather than reading <c>DateTimeOffset.UtcNow</c> internally
    /// and defaulting to an empty set) so this is the single, unambiguous
    /// constructor an EF Core Infrastructure implementation can bind to when
    /// materializing an existing role from storage - see the identical
    /// <c>Session</c>/<c>RefreshSession</c>/<c>User</c> reasoning in
    /// <c>AuthenticationInfrastructure.md</c>/<c>AuthenticationIntegration.md</c>.
    /// </summary>
    private Role(RoleId id, RoleName name, DateTimeOffset createdAtUtc, IReadOnlyCollection<PermissionId> permissionIds)
    {
        Id = id;
        Name = name;
        CreatedAtUtc = createdAtUtc;
        _permissionIds = [.. permissionIds];
    }

    /// <summary>Creates a new role with no permissions granted.</summary>
    public static Role Create(RoleName name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var now = DateTimeOffset.UtcNow;
        var role = new Role(RoleId.New(), name, now, []);
        role.AddDomainEvent(new RoleCreated(role.Id, role.Name, now));
        return role;
    }

    /// <summary>Renames the role. A no-op (no event raised) if unchanged.</summary>
    public void Rename(RoleName name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (Name == name) return;

        Name = name;
        AddDomainEvent(new RoleRenamed(Id, name, DateTimeOffset.UtcNow));
    }

    /// <summary>Grants a permission to this role.</summary>
    /// <exception cref="IdentityDomainException">The permission is already granted.</exception>
    public void AddPermission(PermissionId permissionId)
    {
        if (!_permissionIds.Add(permissionId))
            throw IdentityDomainException.PermissionAlreadyAssignedToRole(Id, permissionId);

        AddDomainEvent(new PermissionAssignedToRole(Id, permissionId, DateTimeOffset.UtcNow));
    }

    /// <summary>Revokes a permission from this role.</summary>
    /// <exception cref="IdentityDomainException">The permission is not currently granted.</exception>
    public void RemovePermission(PermissionId permissionId)
    {
        if (!_permissionIds.Remove(permissionId))
            throw IdentityDomainException.PermissionNotAssignedToRole(Id, permissionId);

        AddDomainEvent(new PermissionRemovedFromRole(Id, permissionId, DateTimeOffset.UtcNow));
    }
}
