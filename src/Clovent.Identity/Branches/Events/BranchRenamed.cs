using Clovent.Domain;
using Clovent.Identity.Branches.ValueObjects;

namespace Clovent.Identity.Branches.Events;

/// <summary>Raised when a <see cref="Branch"/>'s name changes.</summary>
public sealed record BranchRenamed(BranchId BranchId, BranchName Name, DateTimeOffset OccurredOnUtc) : IDomainEvent;
