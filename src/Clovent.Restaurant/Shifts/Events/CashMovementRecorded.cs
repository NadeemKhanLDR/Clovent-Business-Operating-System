using System;
using Clovent.Domain;
using Clovent.Identity.Users;

namespace Clovent.Restaurant.Shifts.Events;

/// <summary>Raised when a cash movement (in/out) is recorded on a shift.</summary>
public sealed record CashMovementRecorded(
    ShiftId ShiftId,
    CashMovementId CashMovementId,
    CashMovementType Type,
    decimal Amount,
    string Reason,
    UserId UserId,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;
