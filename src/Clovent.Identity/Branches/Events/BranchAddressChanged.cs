using Clovent.Domain;
using Clovent.Identity.Shared.ValueObjects;

namespace Clovent.Identity.Branches.Events;

/// <summary>Raised when a <see cref="Branch"/>'s address changes.</summary>
public sealed record BranchAddressChanged(BranchId BranchId, Address Address, DateTimeOffset OccurredOnUtc) : IDomainEvent;
