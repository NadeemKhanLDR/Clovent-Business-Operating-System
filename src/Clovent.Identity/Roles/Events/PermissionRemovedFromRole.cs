using Clovent.Domain;
using Clovent.Identity.Permissions;

namespace Clovent.Identity.Roles.Events;

/// <summary>Raised when a <see cref="Permission"/> is removed from a <see cref="Role"/>.</summary>
public sealed record PermissionRemovedFromRole(RoleId RoleId, PermissionId PermissionId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
