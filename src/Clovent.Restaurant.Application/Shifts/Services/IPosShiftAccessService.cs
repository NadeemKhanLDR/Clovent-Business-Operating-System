using System;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Restaurant.Application.Shifts.Dtos;

namespace Clovent.Restaurant.Application.Shifts.Services;

/// <summary>Status outcome of evaluating cashier shift access to the POS terminal.</summary>
public enum PosShiftAccessStatus
{
    /// <summary>Current authenticated cashier already owns the active open shift on this terminal.</summary>
    ExistingOwnShift,

    /// <summary>No shift is currently open on this terminal and user has no open shift elsewhere; cashier must open a shift.</summary>
    ShiftRequired,

    /// <summary>Terminal currently has an open shift owned by a different cashier; entry blocked until that shift is closed.</summary>
    TerminalOccupiedByAnotherUser,

    /// <summary>Current cashier already owns an active open shift on another terminal; entry blocked.</summary>
    UserHasShiftOnAnotherTerminal,

    /// <summary>No POS terminal is configured or identifiable for this workstation.</summary>
    NoTerminalConfigured,

    /// <summary>User is not authenticated or lacks permission to access Restaurant POS.</summary>
    AccessDenied
}

/// <summary>Detailed outcome returned by <see cref="IPosShiftAccessService"/>.</summary>
public sealed record PosShiftAccessResult(
    PosShiftAccessStatus Status,
    ShiftDto? CurrentShift = null,
    Guid? TerminalId = null,
    string? TerminalName = null,
    string? TerminalCode = null,
    Guid? BranchId = null,
    string? BranchName = null,
    Guid? WarehouseId = null,
    string? WarehouseName = null,
    string? OccupyingCashierName = null,
    int? OccupyingShiftNumber = null,
    DateTimeOffset? OccupyingOpenedAtUtc = null,
    Guid? OccupyingTerminalId = null,
    string? OccupyingTerminalName = null,
    string? OccupyingTerminalCode = null,
    DateOnly? BusinessDate = null,
    string? Message = null);

/// <summary>
/// Domain-aware service that evaluates whether the authenticated user may enter Restaurant POS,
/// inspecting terminal identity, branch context, and open shift ownership.
/// </summary>
public interface IPosShiftAccessService
{
    /// <summary>Evaluates current terminal, cashier, and shift context to determine POS entry eligibility.</summary>
    Task<PosShiftAccessResult> EvaluateAccessAsync(
        Guid cashierId,
        string cashierName,
        Guid? terminalId,
        string? terminalName = null,
        string? terminalCode = null,
        Guid? branchId = null,
        string? branchName = null,
        Guid? warehouseId = null,
        string? warehouseName = null,
        CancellationToken cancellationToken = default);
}
