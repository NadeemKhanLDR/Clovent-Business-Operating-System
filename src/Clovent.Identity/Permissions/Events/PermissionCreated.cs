using Clovent.Domain;
using Clovent.Identity.Permissions.ValueObjects;

namespace Clovent.Identity.Permissions.Events;

/// <summary>Raised when a new <see cref="Permission"/> is created.</summary>
public sealed record PermissionCreated(PermissionId PermissionId, PermissionCode Code, DateTimeOffset OccurredOnUtc) : IDomainEvent;
