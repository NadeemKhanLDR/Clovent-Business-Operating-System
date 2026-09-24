using System;
using Clovent.Domain;
using Clovent.Identity.Branches;
using Clovent.Identity.Users;

namespace Clovent.Restaurant.DayClose.Events;

/// <summary>Domain event raised when a restaurant location's business day is officially reconciled and closed.</summary>
public sealed record BusinessDayClosed(
    BusinessDayCloseId Id,
    BranchId BranchId,
    DateOnly BusinessDate,
    decimal TotalSales,
    decimal CashSales,
    decimal CardSales,
    int ShiftCount,
    decimal TotalShiftVariance,
    UserId ClosedByUserId,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;
