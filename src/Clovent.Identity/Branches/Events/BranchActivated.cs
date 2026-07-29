using Clovent.Domain;

namespace Clovent.Identity.Branches.Events;

/// <summary>Raised when a <see cref="Branch"/> is (re)activated.</summary>
public sealed record BranchActivated(BranchId BranchId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
