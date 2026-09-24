using System;
using System.Collections.Generic;

namespace Clovent.Restaurant.Application.DayClose.Dtos;

/// <summary>Summary representation of a shift included in business day closing.</summary>
public sealed record ShiftDayCloseItemDto(
    Guid ShiftId,
    int ShiftNumber,
    string CashierName,
    Guid TerminalId,
    DateTimeOffset OpenedAtUtc,
    DateTimeOffset? ClosedAtUtc,
    string Status,
    decimal StartingCash,
    decimal ExpectedCash,
    decimal CountedCash,
    decimal CashVariance,
    string? VarianceReason);

/// <summary>
/// Comprehensive summary of a business day for manager review prior to or after closing.
/// </summary>
public sealed record BusinessDaySummaryDto(
    Guid BranchId,
    DateOnly BusinessDate,
    bool IsAlreadyClosed,
    BusinessDayCloseDto? ExistingClose,
    IReadOnlyList<ShiftDayCloseItemDto> OpenShifts,
    IReadOnlyList<ShiftDayCloseItemDto> ClosedShifts,
    int TotalShifts,
    int TotalOrders,
    decimal TotalSales,
    decimal CashSales,
    decimal CardSales,
    decimal OtherSales,
    decimal Refunds,
    decimal Discounts,
    decimal Tax,
    decimal CashIn,
    decimal CashOut,
    decimal TotalShiftVariance,
    bool CanClose,
    string? BlockingReason = null);
