using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Navigation;
using Clovent.Desktop.Restaurant.Orders;
using Clovent.Desktop.Restaurant.Shifts;
using Clovent.Desktop.Sessions;
using Clovent.Restaurant.Application.Shifts.Dtos;
using Clovent.Restaurant.Application.Shifts.Services;
using Clovent.Restaurant.Continuity;
using DevExpress.XtraEditors;
using MediatR;

namespace Clovent.Desktop.Restaurant.Services;

/// <summary>
/// Canonical implementation of <see cref="IPosEntryGateCoordinator"/>.
/// Enforces that POS is NEVER entered without a resolved terminal and valid active cashier shift,
/// and provides a safe, controlled offline Continuity Mode startup path when the primary database is unavailable.
/// </summary>
public sealed class PosEntryGateCoordinator(
    ITerminalResolutionService terminalResolutionService,
    IPosShiftAccessService posShiftAccessService,
    IApplicationModeNavigator applicationModeNavigator,
    IMediator mediator,
    ICurrentSession currentSession,
    Clovent.Restaurant.Application.Attendance.Services.IAttendanceAccessService? attendanceAccessService = null,
    IContinuityCoordinator? continuityCoordinator = null) : IPosEntryGateCoordinator
{
    internal static Action<IWin32Window?, string, string, MessageBoxButtons, MessageBoxIcon>? CustomMessageBoxShow { get; set; }
    internal static Func<IWin32Window?, Form, DialogResult>? CustomDialogShow { get; set; }

    /// <inheritdoc/>
    public async Task<bool> EnsureShiftAndOpenPosAsync(IWin32Window? owner = null)
    {
        owner ??= applicationModeNavigator.CurrentForm;

        // 0. Pre-flight Database Health Check
        if (continuityCoordinator != null)
        {
            var isDbUp = await continuityCoordinator.CheckDatabaseHealthAsync().ConfigureAwait(false);
            if (!isDbUp)
            {
                return await TryStartContinuityModeAsync(owner, null).ConfigureAwait(false);
            }
        }

        try
        {
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

            // Update terminal context on coordinator and trigger background cache sync
            if (continuityCoordinator != null && terminalRes.TerminalId.HasValue)
            {
                continuityCoordinator.SetTerminalContext(
                    companyId: Guid.Empty,
                    companyName: "Default Company",
                    branchId: terminalRes.BranchId ?? Guid.Empty,
                    branchName: terminalRes.BranchName ?? "Main Branch",
                    terminalId: terminalRes.TerminalId.Value,
                    terminalName: terminalRes.TerminalName ?? "POS Station",
                    terminalCode: terminalRes.TerminalCode ?? "POS",
                    warehouseId: terminalRes.WarehouseId ?? Guid.Empty,
                    warehouseName: terminalRes.WarehouseName ?? "Main Warehouse",
                    currencyCode: "PKR",
                    currencySymbol: "Rs.",
                    currencyDecimals: 2);

                _ = Task.Run(() => continuityCoordinator.RefreshCacheAsync());
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

                    var punchResult = CustomDialogShow != null
                        ? CustomDialogShow(owner, punchInDialog)
                        : (!Application.MessageLoop && owner == null ? DialogResult.Cancel : punchInDialog.ShowDialog(owner));
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
                        var dialogResult = CustomDialogShow != null
                            ? CustomDialogShow(owner, openDialog)
                            : (!Application.MessageLoop && owner == null ? DialogResult.Cancel : openDialog.ShowDialog(owner));
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
                        accessRes.Message ?? "This terminal has an active shift opened by another user.",
                        "Terminal Shift Already Open");
                    return false;

                case PosShiftAccessStatus.UserHasShiftOnAnotherTerminal:
                    ShowWarning(
                        owner,
                        accessRes.Message ?? "You already have an active shift open on another terminal.",
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
        catch (Exception)
        {
            // Database connectivity failed during gate resolution; attempt safe continuity mode startup
            if (continuityCoordinator != null)
            {
                return await TryStartContinuityModeAsync(owner, null).ConfigureAwait(false);
            }
            throw;
        }
    }

    private async Task<bool> TryStartContinuityModeAsync(IWin32Window? owner, TerminalResolutionResult? terminalRes)
    {
        if (continuityCoordinator == null) return false;

        var termId = terminalRes?.TerminalId ?? PosSettingsStore.LoadTerminalId() ?? Guid.Empty;
        var branchId = terminalRes?.BranchId ?? PosSettingsStore.LoadBranchId() ?? Guid.Empty;

        // If context not set on coordinator, attempt best-effort config from PosSettingsStore
        if (continuityCoordinator.CurrentTerminalId == Guid.Empty && termId != Guid.Empty)
        {
            continuityCoordinator.SetTerminalContext(
                Guid.Empty, "Default Company", branchId, "Default Branch", termId, "Default Terminal", "POS01",
                Guid.Empty, "Main Warehouse", "PKR", "Rs.", 2);
        }

        var (status, msg) = await continuityCoordinator.ValidateCacheAsync().ConfigureAwait(false);
        if (status is CacheValidationStatus.Valid or CacheValidationStatus.StaleWithinPolicy)
        {
            var snapshot = continuityCoordinator.ActiveCache;
            var resolvedTermId = snapshot?.Metadata.TerminalId ?? termId;
            var resolvedBranchId = snapshot?.Metadata.BranchId ?? branchId;
            var resolvedWhId = snapshot?.Metadata.WarehouseId ?? Guid.Empty;

            var cashierId = currentSession.UserId ?? Guid.Parse("00000000-0000-0000-0000-000000000001");
            var cashierName = Clovent.Desktop.Forms.Base.UserDisplayNameHelper.GetCurrentCashierDisplayName(currentSession);

            var offlineShift = new ShiftDto(
                Guid.NewGuid(),
                0,
                resolvedBranchId,
                resolvedWhId,
                resolvedTermId,
                cashierId,
                cashierName,
                DateTimeOffset.UtcNow,
                null,
                "Open",
                0m,
                0m,
                0m,
                0m,
                null,
                "Emergency Continuity Shift (Offline)",
                DateTimeOffset.UtcNow);

            continuityCoordinator.EnterContinuityMode("Primary database is unreachable at startup. Operating in Continuity Mode.");

            ShowWarning(
                owner,
                "Primary database is currently unreachable.\n\nOpening Restaurant POS in Continuity Mode (Cash Only).\nTransactions are secured in the encrypted local journal and will synchronize upon network restoration.",
                "Database Offline - Continuity Mode");

            await OpenPosWithShiftAsync(offlineShift).ConfigureAwait(false);
            return true;
        }

        ShowError(
            owner,
            $"Primary database is unreachable and local operational cache cannot be used safely.\n\nCache Status: {status}\nDetails: {msg}\n\nOffline selling is blocked for financial safety. Please check network/database connectivity or contact your manager.",
            "Continuity Mode Unavailable");
        return false;
    }

    private static void ShowWarning(IWin32Window? owner, string message, string caption)
    {
        if (CustomMessageBoxShow != null)
        {
            CustomMessageBoxShow(owner, message, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!Application.MessageLoop && owner == null)
        {
            return;
        }

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
        if (CustomMessageBoxShow != null)
        {
            CustomMessageBoxShow(owner, message, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (!Application.MessageLoop && owner == null)
        {
            return;
        }

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
