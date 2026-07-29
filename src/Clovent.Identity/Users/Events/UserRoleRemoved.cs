using Clovent.Domain;
using Clovent.Identity.Roles;

namespace Clovent.Identity.Users.Events;

/// <summary>Raised when a <see cref="Role"/> is removed from a <see cref="User"/>.</summary>
public sealed record UserRoleRemoved(UserId UserId, RoleId RoleId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
