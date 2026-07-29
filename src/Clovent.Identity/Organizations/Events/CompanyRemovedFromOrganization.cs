using Clovent.Domain;
using Clovent.Identity.Companies;

namespace Clovent.Identity.Organizations.Events;

/// <summary>Raised when a <see cref="Company"/> is removed from an <see cref="Organization"/>.</summary>
public sealed record CompanyRemovedFromOrganization(OrganizationId OrganizationId, CompanyId CompanyId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
