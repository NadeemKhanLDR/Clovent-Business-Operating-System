using Clovent.Domain;
using Clovent.Identity.Roles.ValueObjects;

namespace Clovent.Identity.Roles.Events;

/// <summary>Raised when a <see cref="Role"/>'s name changes.</summary>
public sealed record RoleRenamed(RoleId RoleId, RoleName NewName, DateTimeOffset OccurredOnUtc) : IDomainEvent;
