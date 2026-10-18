using System.Drawing;
using System.Text.Json;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Printing;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Platform.Printing;
using Clovent.Restaurant.Application.ActivityLogs.Commands;
using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Application.Printing;
using Clovent.Restaurant.Orders;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>
/// Post-checkout completion dialog displaying finalized invoice number, financial totals,
/// change due, spooler status, and providing authorized receipt printing and reprint auditing.
/// Never alters or recalculates immutable snapshot data.
/// </summary>
public partial class OrderCompletedDialog : XtraForm
{
    private readonly OrderDto? _order;
    private readonly decimal _grandTotal;
    private readonly decimal _totalTendered;
    private readonly decimal _changeDue;
    private readonly IMediator? _mediator;
    private readonly PrinterManagementService? _printerService;
    private readonly IReceiptPrintService? _receiptPrintService;
    private readonly IPrinterJobTracker? _jobTracker;
    private readonly IAuthorizationService? _authService;
    private readonly ICurrentSession? _currentSession;
    private readonly ILogger? _logger;

    /// <summary>Parameterless constructor for Visual Studio WinForms Designer.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public OrderCompletedDialog() : this(null, 0, 0, 0, null, null, null, null, null, null, null)
    {
    }

    /// <summary>Constructs the order completion dialog with finalized order snapshot and payment details.</summary>
    public OrderCompletedDialog(
        OrderDto? order,
        decimal grandTotal,
        decimal totalTendered,
        decimal changeDue,
        IMediator? mediator = null,
        PrinterManagementService? printerService = null,
        IReceiptPrintService? receiptPrintService = null,
        IPrinterJobTracker? jobTracker = null,
        IAuthorizationService? authService = null,
        ICurrentSession? currentSession = null,
        ILogger? logger = null)
    {
        InitializeComponent();

        _order = order;
        _grandTotal = grandTotal;
        _totalTendered = totalTendered;
        _changeDue = changeDue;
        _mediator = mediator;
        _printerService = printerService;
        _receiptPrintService = receiptPrintService;
        _jobTracker = jobTracker;
        _authService = authService;
        _currentSession = currentSession;
        _logger = logger;

        if (DesignModeHelper.IsInDesignMode)
        {
            return;
        }

        PopulateDisplay();
        Load += OrderCompletedDialog_Load;
        btnPrintReceipt.Click += BtnPrintReceipt_Click;
        btnReprintReceipt.Click += BtnReprintReceipt_Click;
    }

    private void OrderCompletedDialog_Load(object? sender, EventArgs e)
    {
        if (DesignModeHelper.IsInDesignMode)
        {
            return;
        }

        ApplyDpiScaling();
        PopulateDisplay();
    }

    private void ApplyDpiScaling()
    {
        ClientSize = new Size(
            DesktopDpi.Scale(560, this),
            DesktopDpi.Scale(400, this));
        MinimumSize = ClientSize;
    }

    private void PopulateDisplay()
    {
        var orderNum = _order?.OrderNumber ?? "N/A";
        lblOrderNumber.Text = $"Order: #{orderNum}";

        var saleNum = _order?.DailySalesNumber?.ToString() ?? "--";
        lblInvoiceNumber.Text = $"Sale / Invoice #: {saleNum}";

        lblGrandTotal.Text = $"Grand Total: {CurrencyDisplay.Format(_grandTotal)}";
        lblTendered.Text = $"Total Tendered: {CurrencyDisplay.Format(_totalTendered)}";
        lblChangeDue.Text = $"Change Due: {CurrencyDisplay.Format(_changeDue)}";

        var reprintCount = _jobTracker?.GetReprintCount(orderNum) ?? 0;
        lblReprintInfo.Text = $"Historical Reprints: {reprintCount} copies";
    }

    private PrintableReceiptData? GetPrintableData()
    {
        if (_order == null) return null;

        if (!string.IsNullOrWhiteSpace(_order.ReceiptSnapshotJson))
        {
            try
            {
                var snapshot = JsonSerializer.Deserialize<ReceiptSnapshot>(_order.ReceiptSnapshotJson);
                if (snapshot != null)
                {
                    return snapshot.ToPrintable();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to deserialize immutable receipt snapshot for order {OrderNumber}", _order.OrderNumber);
            }
        }

        // Fallback for in-memory completion before snapshot sync
        return new PrintableReceiptData(
            OrderId: _order.OrderId,
            OrderNumber: _order.OrderNumber,
            DailySalesNumber: _order.DailySalesNumber,
            OrderType: _order.OrderType ?? "DineIn",
            CompletedAtUtc: _order.UpdatedAtUtc,
            Items: [],
            Subtotal: _grandTotal,
            TaxTotal: 0m,
            DiscountTotal: 0m,
            ServiceChargeTotal: 0m,
            GrandTotal: _grandTotal,
            Balance: 0m,
            Payments: [new("Tender", _totalTendered)],
            CashierName: _currentSession?.DisplayName ?? "Cashier");
    }

    private async void BtnPrintReceipt_Click(object? sender, EventArgs e)
    {
        var printable = GetPrintableData();
        if (printable == null)
        {
            XtraMessageBox.Show(this, "No receipt data available for this order.", "Receipt Unavailable", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var terminalId = PosSettingsStore.LoadTerminalId();
        var branchId = PosSettingsStore.LoadBranchId();
        var settings = _printerService != null
            ? await _printerService.GetTerminalReceiptSettingsAsync(terminalId)
            : new TerminalReceiptSettings();

        var options = new ReceiptRenderOptions(
            PaperWidth: settings.PaperWidth,
            CharactersPerLine: settings.CharactersPerLine,
            CopyType: ReceiptCopyType.Customer,
            MarginMm: settings.MarginMm,
            HeaderText: settings.HeaderText,
            FooterText: settings.FooterText,
            LogoPath: settings.LogoPath,
            EnableLogo: settings.EnableLogo);

        var formatted = ReceiptSnapshotFormatter.Format(printable, options);

        if (_printerService != null)
        {
            var dispatchResult = await _printerService.DispatchReceiptAsync(
                printable,
                organizationId: null,
                branchId: branchId,
                terminalId: terminalId,
                isReprint: false,
                copyType: ReceiptCopyType.Customer,
                customSettings: settings);

            if (dispatchResult.Success)
            {
                lblPrintStatus.Text = $"Customer copy submitted to Windows spooler ({dispatchResult.TargetQueueName ?? "Default Queue"}).";
            }
            else
            {
                lblPrintStatus.Text = $"Spooler dispatch issue: {dispatchResult.ErrorMessage}";
            }
        }

        using var preview = new ReceiptPreviewForm(formatted.FormattedText);
        preview.Text = $"Customer Receipt Preview - {_order?.OrderNumber}";
        preview.ShowDialog(this);
    }

    private async void BtnReprintReceipt_Click(object? sender, EventArgs e)
    {
        // 1. Authorization check
        var isAuthorized = true;
        if (_authService != null && _currentSession?.UserId is { } userId)
        {
            isAuthorized = await _authService.HasPermissionAsync(userId, "feature.orders.reprint")
                || await _authService.HasRoleAsync(userId, "Administrator")
                || await _authService.HasRoleAsync(userId, "Manager");
        }

        if (!isAuthorized)
        {
            XtraMessageBox.Show(
                this,
                "Manager authorization is required to reprint finalized customer receipts.",
                "Reprint Authorization Required",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        // 1.1 Scope Ownership Enforcement: Manager/Admin cannot bypass company/branch/terminal boundaries
        if (_order != null && !string.IsNullOrWhiteSpace(_order.ReceiptSnapshotJson))
        {
            try
            {
                var snapshot = JsonSerializer.Deserialize<ReceiptSnapshot>(_order.ReceiptSnapshotJson);
                if (snapshot != null)
                {
                    try
                    {
                        PosSettingsStore.EnsureWorkstationScopeOwnership(
                            snapshot.OrganizationId,
                            snapshot.BranchId,
                            snapshot.TerminalId,
                            "reprint customer receipt");
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        XtraMessageBox.Show(
                            this,
                            ex.Message,
                            "Scope Violation",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to verify scope ownership on snapshot for order {OrderNumber}", _order.OrderNumber);
            }
        }

        // 2. Reason prompt
        var reason = XtraInputBox.Show(
            "Enter reason for receipt reprint (e.g. Customer Requested Duplicate, Paper Jam, Accounting Audit):",
            "Receipt Reprint Reason",
            "Customer Request");

        if (string.IsNullOrWhiteSpace(reason))
        {
            return; // Cancelled
        }

        var printable = GetPrintableData();
        if (printable == null)
        {
            XtraMessageBox.Show(this, "No receipt snapshot available to reprint.", "Reprint Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var orderNum = _order?.OrderNumber ?? "N/A";
        var reprintCount = (_jobTracker?.GetReprintCount(orderNum) ?? 0) + 1;
        var terminalId = PosSettingsStore.LoadTerminalId();
        var branchId = PosSettingsStore.LoadBranchId();

        var reprintJob = new PrinterJob
        {
            JobId = Guid.NewGuid(),
            DocumentId = _order?.OrderId,
            DocumentReference = orderNum,
            DocumentType = "ReprintReceipt",
            IsReprint = true,
            ReprintCount = reprintCount,
            ReprintReason = reason,
            Status = PrinterJobStatus.Requested,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            BranchId = branchId,
            TerminalId = terminalId
        };
        _jobTracker?.RecordJob(reprintJob);

        var settings = _printerService != null
            ? await _printerService.GetTerminalReceiptSettingsAsync(terminalId)
            : new TerminalReceiptSettings();

        // Strictly suppress cash drawer on reprints
        var options = new ReceiptRenderOptions(
            PaperWidth: settings.PaperWidth,
            CharactersPerLine: settings.CharactersPerLine,
            IsReprint: true,
            ReprintCount: reprintCount,
            ReprintReason: reason,
            CopyType: ReceiptCopyType.Reprint,
            MarginMm: settings.MarginMm,
            HeaderText: settings.HeaderText,
            FooterText: settings.FooterText,
            LogoPath: settings.LogoPath,
            EnableLogo: settings.EnableLogo);

        var formatted = ReceiptSnapshotFormatter.Format(printable, options);

        // Audit the reprint event
        if (_mediator != null)
        {
            try
            {
                await _mediator.Send(new RecordActivityCommand(
                    "Receipt Reprinted",
                    $"Order #{orderNum} reprinted (Copy #{reprintCount}). Reason: {reason}",
                    _currentSession?.DisplayName ?? "Cashier",
                    Environment.MachineName));
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to log activity for receipt reprint of order {OrderNumber}", orderNum);
            }
        }

        // Dispatch to spooler with drawer kick strictly suppressed
        if (_printerService != null)
        {
            var dispatchResult = await _printerService.DispatchReceiptAsync(
                printable,
                organizationId: null,
                branchId: branchId,
                terminalId: terminalId,
                isReprint: true,
                reprintCount: reprintCount,
                reprintReason: reason,
                jobId: reprintJob.JobId,
                copyType: ReceiptCopyType.Reprint,
                customSettings: settings);

            if (dispatchResult.Success)
            {
                lblPrintStatus.Text = $"Reprint #{reprintCount} submitted to Windows spooler ({dispatchResult.TargetQueueName ?? "Default Queue"}).";
            }
        }

        lblReprintInfo.Text = $"Historical Reprints: {reprintCount} copies";

        using var preview = new ReceiptPreviewForm(formatted.FormattedText);
        preview.Text = $"Reprint Copy #{reprintCount} Preview - {orderNum}";
        preview.ShowDialog(this);
    }
}
