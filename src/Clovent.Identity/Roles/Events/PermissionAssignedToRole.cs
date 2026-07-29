using Clovent.Domain;
using Clovent.Identity.Permissions;

namespace Clovent.Identity.Roles.Events;

/// <summary>Raised when a <see cref="Permission"/> is added to a <see cref="Role"/>.</summary>
public sealed record PermissionAssignedToRole(RoleId RoleId, PermissionId PermissionId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
