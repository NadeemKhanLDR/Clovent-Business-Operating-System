using Clovent.Domain;
using Clovent.Identity.Companies.ValueObjects;
using Clovent.Identity.Organizations;

namespace Clovent.Identity.Companies.Events;

/// <summary>Raised when a new <see cref="Company"/> is created under an <see cref="Organization"/>.</summary>
public sealed record CompanyCreated(CompanyId CompanyId, OrganizationId OrganizationId, CompanyName Name, DateTimeOffset OccurredOnUtc) : IDomainEvent;
