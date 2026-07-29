using Clovent.Domain;
using Clovent.Identity.Organizations.ValueObjects;

namespace Clovent.Identity.Organizations.Events;

/// <summary>Raised when a new <see cref="Organization"/> is created.</summary>
public sealed record OrganizationCreated(OrganizationId OrganizationId, OrganizationName Name, DateTimeOffset OccurredOnUtc) : IDomainEvent;
