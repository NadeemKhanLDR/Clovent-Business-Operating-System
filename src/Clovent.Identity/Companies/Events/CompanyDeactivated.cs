using Clovent.Domain;

namespace Clovent.Identity.Companies.Events;

/// <summary>Raised when a <see cref="Company"/> is deactivated.</summary>
public sealed record CompanyDeactivated(CompanyId CompanyId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
