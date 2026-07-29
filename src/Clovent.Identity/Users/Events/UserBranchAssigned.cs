using Clovent.Domain;
using Clovent.Identity.Branches;

namespace Clovent.Identity.Users.Events;

/// <summary>Raised when a <see cref="User"/> is assigned to a <see cref="Branch"/>.</summary>
public sealed record UserBranchAssigned(UserId UserId, BranchId BranchId, DateTimeOffset OccurredOnUtc) : IDomainEvent;
