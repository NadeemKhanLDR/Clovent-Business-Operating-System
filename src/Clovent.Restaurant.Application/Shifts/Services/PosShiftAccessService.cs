using System;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Identity.Users;
using Clovent.MasterData.Terminals;
using Clovent.Restaurant.Application.Shifts.Dtos;
using Clovent.Restaurant.Shifts;
using Microsoft.Extensions.Logging;

namespace Clovent.Restaurant.Application.Shifts.Services;

/// <summary>
/// Canonical implementation of <see cref="IPosShiftAccessService"/>.
/// Checks terminal active shift, current cashier active shift, and determines POS entry permissions.
/// Follows deterministic evaluation order:
/// 1. Same user + same terminal + open shift -> ExistingOwnShift (Auto-resume)
/// 2. Different user + same terminal + open shift -> TerminalOccupiedByAnotherUser (Block)
/// 3. Same user + different terminal + open shift -> UserHasShiftOnAnotherTerminal (Block with terminal context)
/// 4. No active shift -> ShiftRequired (Open Shift dialog)
/// </summary>
public sealed class PosShiftAccessService(
    IShiftRepository shiftRepository,
    IBusinessDateProvider businessDateProvider,
    ITerminalRepository? terminalRepository = null,
    ILogger<PosShiftAccessService>? logger = null) : IPosShiftAccessService
{
    /// <inheritdoc/>
    public async Task<PosShiftAccessResult> EvaluateAccessAsync(
        Guid cashierId,
        string cashierName,
        Guid? terminalId,
        string? terminalName = null,
        string? terminalCode = null,
        Guid? branchId = null,
        string? branchName = null,
        Guid? warehouseId = null,
        string? warehouseName = null,
        CancellationToken cancellationToken = default)
    {
        if (cashierId == Guid.Empty)
        {
            logger?.LogWarning("POS Shift Access: Access denied because cashier identity is empty.");
            return new PosShiftAccessResult(
                PosShiftAccessStatus.AccessDenied,
                Message: "User is not authenticated or cashier identity is invalid.");
        }

        if (terminalId is null || terminalId.Value == Guid.Empty)
        {
            logger?.LogWarning("POS Shift Access: No terminal configured for cashier '{CashierName}'.", cashierName);
            return new PosShiftAccessResult(
                PosShiftAccessStatus.NoTerminalConfigured,
                Message: "No POS terminal is configured for this workstation.");
        }

        var businessDate = businessDateProvider.GetCurrentBusinessDate();
        var termId = new TerminalId(terminalId.Value);
        var userId = new UserId(cashierId);

        // A. Evaluate open shift for current terminal
        var currentTerminalShift = await shiftRepository.GetActiveShiftForTerminalAsync(termId, cancellationToken);

        // B. Evaluate open shift for current cashier
        var currentUserShift = await shiftRepository.GetActiveShiftForCashierAsync(userId, cancellationToken);

        // 1. Same User + Same Terminal + Open Shift -> Auto Resume
        if (currentTerminalShift != null && currentTerminalShift.CashierId == userId)
        {
            logger?.LogInformation(
                "POS Shift Access [Resume]: Cashier '{CashierName}' owns active Shift #{ShiftNumber} on Terminal '{TerminalCode}'. Auto-resuming.",
                cashierName, currentTerminalShift.ShiftNumber, terminalCode ?? terminalName ?? termId.Value.ToString());

            return new PosShiftAccessResult(
                PosShiftAccessStatus.ExistingOwnShift,
                CurrentShift: ShiftDto.FromDomain(currentTerminalShift),
                TerminalId: terminalId,
                TerminalName: terminalName,
                TerminalCode: terminalCode,
                BranchId: branchId,
                BranchName: branchName,
                WarehouseId: warehouseId,
                WarehouseName: warehouseName,
                BusinessDate: businessDate,
                Message: $"Resuming existing Shift #{currentTerminalShift.ShiftNumber}.");
        }

        // 2. Different User + Same Terminal + Open Shift -> Block entry
        if (currentTerminalShift != null && currentTerminalShift.CashierId != userId)
        {
            logger?.LogWarning(
                "POS Shift Access [TerminalOccupied]: Terminal '{TerminalCode}' is occupied by Cashier '{OccupyingCashier}' (Shift #{ShiftNumber}).",
                terminalCode ?? terminalName ?? termId.Value.ToString(), currentTerminalShift.CashierName, currentTerminalShift.ShiftNumber);

            return new PosShiftAccessResult(
                PosShiftAccessStatus.TerminalOccupiedByAnotherUser,
                TerminalId: terminalId,
                TerminalName: terminalName,
                TerminalCode: terminalCode,
                BranchId: branchId,
                BranchName: branchName,
                WarehouseId: warehouseId,
                WarehouseName: warehouseName,
                OccupyingCashierName: currentTerminalShift.CashierName,
                OccupyingShiftNumber: currentTerminalShift.ShiftNumber,
                OccupyingOpenedAtUtc: currentTerminalShift.OpenedAtUtc,
                BusinessDate: businessDate,
                Message: $"Terminal '{terminalName ?? "Terminal"} ({terminalCode ?? termId.Value.ToString()})' currently has an open shift (Shift #{currentTerminalShift.ShiftNumber}) owned by {currentTerminalShift.CashierName}.\n\nThis shift must be closed before another cashier can use this terminal.");
        }

        // 3. Same User + Different Terminal + Open Shift -> Block entry
        if (currentUserShift != null && currentUserShift.TerminalId != termId)
        {
            string otherTermName = "Another Terminal";
            string otherTermCode = currentUserShift.TerminalId.Value.ToString();

            if (terminalRepository != null)
            {
                var otherTerminal = await terminalRepository.GetByIdAsync(currentUserShift.TerminalId, cancellationToken);
                if (otherTerminal != null)
                {
                    otherTermName = otherTerminal.Name.Value;
                    otherTermCode = otherTerminal.Code.Value;
                }
            }

            logger?.LogWarning(
                "POS Shift Access [UserShiftElsewhere]: Cashier '{CashierName}' already has active Shift #{ShiftNumber} on Terminal '{OtherTermCode}'.",
                cashierName, currentUserShift.ShiftNumber, otherTermCode);

            return new PosShiftAccessResult(
                PosShiftAccessStatus.UserHasShiftOnAnotherTerminal,
                TerminalId: terminalId,
                TerminalName: terminalName,
                TerminalCode: terminalCode,
                BranchId: branchId,
                BranchName: branchName,
                WarehouseId: warehouseId,
                WarehouseName: warehouseName,
                OccupyingCashierName: cashierName,
                OccupyingShiftNumber: currentUserShift.ShiftNumber,
                OccupyingOpenedAtUtc: currentUserShift.OpenedAtUtc,
                OccupyingTerminalId: currentUserShift.TerminalId.Value,
                OccupyingTerminalName: otherTermName,
                OccupyingTerminalCode: otherTermCode,
                BusinessDate: businessDate,
                Message: $"Cashier '{cashierName}' already has an active Shift #{currentUserShift.ShiftNumber} open on terminal '{otherTermName} ({otherTermCode})'.\n\nCurrent workstation terminal: '{terminalName ?? "Terminal"} ({terminalCode ?? termId.Value.ToString()})'.\n\nYou must close that shift before opening or resuming a shift on this terminal.");
        }

        // 4. No active shift exists for terminal or cashier -> Open Shift required
        logger?.LogInformation(
            "POS Shift Access [OpenRequired]: No active shift for Terminal '{TerminalCode}' or Cashier '{CashierName}'. Presenting Open Shift.",
            terminalCode ?? terminalName ?? termId.Value.ToString(), cashierName);

        return new PosShiftAccessResult(
            PosShiftAccessStatus.ShiftRequired,
            TerminalId: terminalId,
            TerminalName: terminalName,
            TerminalCode: terminalCode,
            BranchId: branchId,
            BranchName: branchName,
            WarehouseId: warehouseId,
            WarehouseName: warehouseName,
            BusinessDate: businessDate,
            Message: "A cashier shift is required to operate this POS terminal.");
    }
}
