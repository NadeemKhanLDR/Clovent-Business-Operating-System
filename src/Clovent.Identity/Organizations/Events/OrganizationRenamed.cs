using Clovent.Domain;
using Clovent.Identity.Organizations.ValueObjects;

namespace Clovent.Identity.Organizations.Events;

/// <summary>Raised when an <see cref="Organization"/>'s name changes.</summary>
public sealed record OrganizationRenamed(OrganizationId OrganizationId, OrganizationName Name, DateTimeOffset OccurredOnUtc) : IDomainEvent;
