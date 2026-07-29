using Clovent.Domain;
using Clovent.Identity.Companies.ValueObjects;

namespace Clovent.Identity.Companies.Events;

/// <summary>Raised when a <see cref="Company"/>'s name changes.</summary>
public sealed record CompanyRenamed(CompanyId CompanyId, CompanyName Name, DateTimeOffset OccurredOnUtc) : IDomainEvent;
