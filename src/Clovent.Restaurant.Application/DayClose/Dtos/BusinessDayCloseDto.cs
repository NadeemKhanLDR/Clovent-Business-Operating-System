using System;
using Clovent.Restaurant.DayClose;

namespace Clovent.Restaurant.Application.DayClose.Dtos;

/// <summary>Read-model projection of a <see cref="BusinessDayClose"/> aggregate.</summary>
public sealed record BusinessDayCloseDto(
    Guid Id,
    Guid BranchId,
    DateOnly BusinessDate,
    DateTimeOffset ClosedAtUtc,
    Guid ClosedByUserId,
    string ClosedByUserName,
    string Status,
    decimal TotalSales,
    decimal CashSales,
    decimal CardSales,
    decimal OtherPayments,
    decimal Refunds,
    decimal Discounts,
    decimal Tax,
    decimal CashIn,
    decimal CashOut,
    int ShiftCount,
    decimal TotalShiftVariance,
    int OrderCount,
    string? Notes,
    DateTimeOffset CreatedAtUtc)
{
    /// <summary>Projects a domain entity to its DTO representation.</summary>
    public static BusinessDayCloseDto FromDomain(BusinessDayClose entity) => new(
        entity.Id.Value,
        entity.BranchId.Value,
        entity.BusinessDate,
        entity.ClosedAtUtc,
        entity.ClosedByUserId.Value,
        entity.ClosedByUserName,
        entity.Status.ToString(),
        entity.TotalSales,
        entity.CashSales,
        entity.CardSales,
        entity.OtherPayments,
        entity.Refunds,
        entity.Discounts,
        entity.Tax,
        entity.CashIn,
        entity.CashOut,
        entity.ShiftCount,
        entity.TotalShiftVariance,
        entity.OrderCount,
        entity.Notes,
        entity.CreatedAtUtc);
}
