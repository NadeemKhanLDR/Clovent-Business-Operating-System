using Clovent.Domain;
using Clovent.Identity.Roles.ValueObjects;

namespace Clovent.Identity.Roles.Events;

/// <summary>Raised when a new <see cref="Role"/> is created.</summary>
public sealed record RoleCreated(RoleId RoleId, RoleName Name, DateTimeOffset OccurredOnUtc) : IDomainEvent;
