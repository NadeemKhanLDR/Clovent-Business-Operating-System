using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Desktop.Navigation;
using Clovent.Desktop.Restaurant.Orders;
using Clovent.Desktop.Restaurant.Shifts;
using Clovent.Desktop.Sessions;
using Clovent.Restaurant.Application.Shifts.Dtos;
using Clovent.Restaurant.Application.Shifts.Services;
using DevExpress.XtraEditors;
using MediatR;

namespace Clovent.Desktop.Restaurant.Services;

/// <summary>
/// Canonical implementation of <see cref="IPosEntryGateCoordinator"/>.
/// Enforces that POS is NEVER entered without a resolved terminal and valid active cashier shift.
/// </summary>
public sealed class PosEntryGateCoordinator(
    ITerminalResolutionService terminalResolutionService,
    IPosShiftAccessService posShiftAccessService,
    IApplicationModeNavigator applicationModeNavigator,
    IMediator mediator,
    ICurrentSession currentSession,
    Clovent.Restaurant.Application.Attendance.Services.IAttendanceAccessService? attendanceAccessService = null) : IPosEntryGateCoordinator
{
    /// <inheritdoc/>
    public async Task<bool> EnsureShiftAndOpenPosAsync(IWin32Window? owner = null)
    {
        owner ??= applicationModeNavigator.CurrentForm;

        // 1. Resolve Workstation Terminal & Branch Context
        var terminalRes = await terminalResolutionService.ResolveCurrentTerminalAsync();
        if (!terminalRes.IsConfigured || !terminalRes.TerminalId.HasValue)
        {
            ShowWarning(
                owner,
                terminalRes.ErrorMessage ?? "No POS terminal is configured for this workstation.\n\nPlease configure a terminal under Master Data > Terminals.",
                "POS Terminal Required");
            return false;
        }

        var cashierId = currentSession.UserId ?? Guid.Parse("00000000-0000-0000-0000-000000000001");
        var cashierName = Clovent.Desktop.Forms.Base.UserDisplayNameHelper.GetCurrentCashierDisplayName(currentSession);

        // 2. ATTENDANCE GATE: Cashier MUST be punched in before accessing Restaurant POS
        if (attendanceAccessService != null)
        {
            var openAttendance = await attendanceAccessService.GetOpenSessionAsync(cashierId);
            if (openAttendance == null)
            {
                using var punchInDialog = new Clovent.Desktop.Restaurant.Attendance.PunchInDialog(
                    mediator,
                    currentSession,
                    terminalRes.BranchId ?? Guid.Empty,
                    terminalRes.BranchName,
                    terminalRes.TerminalId);

                var punchResult = punchInDialog.ShowDialog(owner);
                if (punchResult != DialogResult.OK || !punchInDialog.PunchedIn)
                {
                    // Cancelled Punch In: remain in Back Office, do NOT open shift or POS
                    return false;
                }
            }
        }

        // 3. POS CASH SHIFT GATE: Evaluate Shift Access via domain application service
        var accessRes = await posShiftAccessService.EvaluateAccessAsync(
            cashierId,
            cashierName,
            terminalRes.TerminalId.Value,
            terminalRes.TerminalName,
            terminalRes.TerminalCode,
            terminalRes.BranchId,
            terminalRes.BranchName,
            terminalRes.WarehouseId,
            terminalRes.WarehouseName);

        switch (accessRes.Status)
        {
            case PosShiftAccessStatus.ExistingOwnShift:
                // Non-destructive advisory if shift has been open for 24+ hours
                if (accessRes.CurrentShift != null)
                {
                    var duration = DateTimeOffset.UtcNow - accessRes.CurrentShift.OpenedAtUtc;
                    if (duration.TotalHours >= 24)
                    {
                        var openedFormatted = Clovent.Desktop.Forms.Base.DateTimeDisplay.FormatDateTime(accessRes.CurrentShift.OpenedAtUtc);
                        ShowWarning(
                            owner,
                            $"Notice: Shift #{accessRes.CurrentShift.ShiftNumber} has been open for {(int)duration.TotalHours} hours (since {openedFormatted}).\n\nIf this shift belongs to a previous business day, consider closing it and opening a fresh shift for accurate daily reconciliation.",
                            "Long-Running Shift Active");
                    }
                }
                // Existing open shift: Resume directly without prompting
                await OpenPosWithShiftAsync(accessRes.CurrentShift!);
                return true;

            case PosShiftAccessStatus.ShiftRequired:
                // Shift required: Present OpenShiftDialog
                using (var openDialog = new OpenShiftDialog(
                    mediator,
                    currentSession,
                    terminalRes.BranchId ?? Guid.Empty,
                    terminalRes.WarehouseId ?? Guid.Empty,
                    terminalRes.TerminalId.Value,
                    terminalRes.TerminalName,
                    terminalRes.BranchName,
                    accessRes.BusinessDate))
                {
                    var dialogResult = openDialog.ShowDialog(owner);
                    if (dialogResult == DialogResult.OK && openDialog.OpenedShift != null)
                    {
                        await OpenPosWithShiftAsync(openDialog.OpenedShift);
                        return true;
                    }
                }
                // Cancelled: stay in Back Office / current window without opening POS
                return false;

            case PosShiftAccessStatus.TerminalOccupiedByAnotherUser:
                ShowWarning(
                    owner,
                    accessRes.Message,
                    "Terminal Shift Already Open");
                return false;

            case PosShiftAccessStatus.UserHasShiftOnAnotherTerminal:
                ShowWarning(
                    owner,
                    accessRes.Message,
                    "Active Shift on Another Terminal");
                return false;

            case PosShiftAccessStatus.NoTerminalConfigured:
                ShowWarning(
                    owner,
                    accessRes.Message ?? "No POS terminal is configured.",
                    "Terminal Required");
                return false;

            case PosShiftAccessStatus.AccessDenied:
            default:
                ShowError(
                    owner,
                    accessRes.Message ?? "You do not have permission to access Restaurant POS.",
                    "Access Denied");
                return false;
        }
    }

    private static void ShowWarning(IWin32Window? owner, string message, string caption)
    {
        if (owner is not null)
        {
            XtraMessageBox.Show(owner, message, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        else
        {
            XtraMessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static void ShowError(IWin32Window? owner, string message, string caption)
    {
        if (owner is not null)
        {
            XtraMessageBox.Show(owner, message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        else
        {
            XtraMessageBox.Show(message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task OpenPosWithShiftAsync(ShiftDto shift)
    {
        await applicationModeNavigator.OpenPosAsync(shift);
    }
}
