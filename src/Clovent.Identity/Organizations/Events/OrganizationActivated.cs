using Clovent.Domain;

namespace Clovent.Identity.Organizations.Events;

/// <summary>Raised when an <see cref="Organization"/> is (re)activated.</summary>
public sealed record OrganizationActivated(OrganizationId OrganizationId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
