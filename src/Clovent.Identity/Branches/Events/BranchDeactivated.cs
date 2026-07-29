using Clovent.Domain;

namespace Clovent.Identity.Branches.Events;

/// <summary>Raised when a <see cref="Branch"/> is deactivated.</summary>
public sealed record BranchDeactivated(BranchId BranchId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
