using System;
using Clovent.Restaurant.Shifts;

namespace Clovent.Restaurant.Application.Shifts.Dtos;

/// <summary>Read-model shape for a <see cref="Shift"/> aggregate.</summary>
public sealed record ShiftDto(
    Guid ShiftId,
    int ShiftNumber,
    Guid BranchId,
    Guid WarehouseId,
    Guid TerminalId,
    Guid CashierId,
    string CashierName,
    DateTimeOffset OpenedAtUtc,
    DateTimeOffset? ClosedAtUtc,
    string Status,
    decimal StartingCash,
    decimal ExpectedCash,
    decimal CountedCash,
    decimal CashVariance,
    string? VarianceReason,
    string? Notes,
    DateTimeOffset CreatedAtUtc)
{
    /// <summary>Projects a domain <see cref="Shift"/> into its DTO.</summary>
    public static ShiftDto FromDomain(Shift shift) => new(
        shift.Id.Value,
        shift.ShiftNumber,
        shift.BranchId.Value,
        shift.WarehouseId.Value,
        shift.TerminalId.Value,
        shift.CashierId.Value,
        shift.CashierName,
        shift.OpenedAtUtc,
        shift.ClosedAtUtc,
        shift.Status.ToString(),
        shift.StartingCash,
        shift.ExpectedCash,
        shift.CountedCash,
        shift.CashVariance,
        shift.VarianceReason,
        shift.Notes,
        shift.CreatedAtUtc);
}
