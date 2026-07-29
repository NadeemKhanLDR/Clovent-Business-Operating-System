using Clovent.Domain;
using Clovent.Identity.Branches.ValueObjects;
using Clovent.Identity.Companies;

namespace Clovent.Identity.Branches.Events;

/// <summary>Raised when a new <see cref="Branch"/> is created under a <see cref="Company"/>.</summary>
public sealed record BranchCreated(BranchId BranchId, CompanyId CompanyId, BranchName Name, DateTimeOffset OccurredOnUtc) : IDomainEvent;
