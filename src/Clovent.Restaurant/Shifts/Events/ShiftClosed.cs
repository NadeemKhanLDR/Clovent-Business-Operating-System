using System;
using Clovent.Domain;

namespace Clovent.Restaurant.Shifts.Events;

/// <summary>Raised when a <see cref="Shift"/> register session is closed.</summary>
public sealed record ShiftClosed(
    ShiftId ShiftId,
    int ShiftNumber,
    decimal ExpectedCash,
    decimal CountedCash,
    decimal Variance,
    string? VarianceReason,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;
