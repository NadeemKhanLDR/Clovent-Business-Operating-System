namespace Clovent.Modules.Identity.Domain.Entities;

public sealed class UserRole
{
    public Guid UserId { get; private set; }

    public Guid RoleId { get; private set; }

    public DateTime AssignedOn { get; private set; } = DateTime.UtcNow;

    private UserRole()
    {
    }

    public UserRole(Guid userId, Guid roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }
}
