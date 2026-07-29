using Clovent.Domain;

namespace Clovent.Identity.Organizations.Events;

/// <summary>Raised when an <see cref="Organization"/> is deactivated.</summary>
public sealed record OrganizationDeactivated(OrganizationId OrganizationId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
