using Clovent.Domain;
using Clovent.Identity.Branches;

namespace Clovent.Identity.Companies.Events;

/// <summary>Raised when a <see cref="Branch"/> is removed from a <see cref="Company"/>.</summary>
public sealed record BranchRemovedFromCompany(CompanyId CompanyId, BranchId BranchId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
