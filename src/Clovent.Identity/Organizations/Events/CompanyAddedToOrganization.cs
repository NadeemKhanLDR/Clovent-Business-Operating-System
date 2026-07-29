using Clovent.Domain;
using Clovent.Identity.Companies;

namespace Clovent.Identity.Organizations.Events;

/// <summary>Raised when a <see cref="Company"/> is added to an <see cref="Organization"/>.</summary>
public sealed record CompanyAddedToOrganization(OrganizationId OrganizationId, CompanyId CompanyId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
