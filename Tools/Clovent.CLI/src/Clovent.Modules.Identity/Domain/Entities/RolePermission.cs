namespace Clovent.Modules.Identity.Domain.Entities;

public sealed class RolePermission
{
    public Guid RoleId { get; private set; }

    public Guid PermissionId { get; private set; }

    public DateTime AssignedOn { get; private set; } = DateTime.UtcNow;

    private RolePermission()
    {
    }

    public RolePermission(Guid roleId, Guid permissionId)
    {
        RoleId = roleId;
        PermissionId = permissionId;
    }
}
