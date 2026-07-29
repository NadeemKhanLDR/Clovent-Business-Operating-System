using Clovent.Domain;

namespace Clovent.Identity.Companies.Events;

/// <summary>Raised when a <see cref="Company"/> is (re)activated.</summary>
public sealed record CompanyActivated(CompanyId CompanyId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
