using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using Clovent.Restaurant.Application.OrderLines.Queries;
using Clovent.Restaurant.Application.Refunds.Commands;
using Clovent.Restaurant.Application.Refunds.Dtos;
using Clovent.Restaurant.Application.Refunds.Queries;
using Clovent.Restaurant.DomainServices;
using Clovent.Restaurant.Refunds;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid.Views.Grid;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>Represents an editable order line item in the refund entry dialog.</summary>
public sealed class RefundLineItemRow
{
    /// <summary>Identity of the original order line.</summary>
    public Guid OrderLineId { get; set; }

    /// <summary>Identity of the product variant.</summary>
    public Guid ProductVariantId { get; set; }

    /// <summary>Name of the menu item.</summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>Original quantity purchased on the order.</summary>
    public decimal OriginalQuantity { get; set; }

    /// <summary>Quantity already refunded on previous credit notes.</summary>
    public decimal AlreadyRefundedQuantity { get; set; }

    /// <summary>Maximum remaining quantity eligible for refund.</summary>
    public decimal RemainingQuantity => Math.Max(0m, OriginalQuantity - AlreadyRefundedQuantity);

    /// <summary>Quantity to refund on this credit note.</summary>
    public decimal RefundQuantity { get; set; }

    /// <summary>Selling unit price from original sale.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Estimated refund total for this line.</summary>
    public decimal EstimatedRefund => MoneyRoundingPolicy.RoundMoney(RefundQuantity * UnitPrice);

    /// <summary>Whether returned stock is restored to warehouse inventory or written off as waste.</summary>
    public InventoryDisposition Disposition { get; set; } = InventoryDisposition.Restock;
}

/// <summary>
/// WinForms dialog for executing full or partial refunds against a completed order.
/// Calculates remaining eligibility, supports line-level restock/waste disposition,
/// captures business reason and settlement method, and prompts for receipt printing.
/// </summary>
[DesignerCategory("Code")]
public sealed partial class RefundOrderDialog : XtraForm
{
    private readonly IMediator _mediator;
    private readonly ILogger? _logger;
    private readonly Guid _orderId;
    private readonly string _orderNumber;
    private readonly Guid _cashierId;
    private readonly string _cashierName;
    private readonly Guid? _branchId;

    private readonly BindingList<RefundLineItemRow> _lineRows = [];

    /// <summary>The resulting refund DTO if successfully processed.</summary>
    public RefundDto? ResultRefund { get; private set; }

    /// <summary>Design-time-only constructor for Visual Studio Designer.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public RefundOrderDialog()
    {
        _mediator = null!;
        _logger = null!;
        _orderNumber = string.Empty;
        _cashierName = string.Empty;

        InitializeComponent();
    }

    /// <summary>Builds the refund dialog for the specified completed order.</summary>
    public RefundOrderDialog(
        IMediator mediator,
        ILogger logger,
        Guid orderId,
        string orderNumber,
        Guid cashierId,
        string cashierName,
        Guid? branchId = null) : base()
    {
        InitializeComponent();

        if (DesignModeHelper.IsInDesignMode)
        {
            _mediator = null!;
            _logger = null!;
            _orderNumber = string.Empty;
            _cashierName = string.Empty;
            return;
        }

        _mediator = mediator;
        _logger = logger;
        _orderId = orderId;
        _orderNumber = orderNumber;
        _cashierId = cashierId;
        _cashierName = cashierName;
        _branchId = branchId;

        _headerLabel.Text = $"Process Refund - {orderNumber}";
    }

    private void AppearanceManager_Changed(object? sender, EventArgs e) =>
        AppearanceManager.Apply(this, "Restaurant", nameof(RefundOrderDialog));

    private async void RefundOrderDialog_Load(object? sender, EventArgs e)
    {
        if (DesignModeHelper.IsInDesignMode)
            return;

        DesktopDialogSizing.Apply(this, 820, 640, 760, 560, null, true);
        await LoadOrderLinesAsync();
    }

    private void WireEvents()
    {
        _linesGrid.DataSource = _lineRows;

        // Configure Grid Columns
        _linesGridView.Columns.Clear();

        var colName = _linesGridView.Columns.AddVisible(nameof(RefundLineItemRow.ItemName), "Item");
        colName.OptionsColumn.AllowEdit = false;
        colName.VisibleIndex = 0;

        var colOrig = _linesGridView.Columns.AddVisible(nameof(RefundLineItemRow.OriginalQuantity), "Ordered");
        colOrig.OptionsColumn.AllowEdit = false;
        colOrig.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
        colOrig.DisplayFormat.FormatString = "N0";
        colOrig.Width = 70;
        colOrig.VisibleIndex = 1;

        var colPrev = _linesGridView.Columns.AddVisible(nameof(RefundLineItemRow.AlreadyRefundedQuantity), "Prev. Refunded");
        colPrev.OptionsColumn.AllowEdit = false;
        colPrev.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
        colPrev.DisplayFormat.FormatString = "N0";
        colPrev.Width = 90;
        colPrev.VisibleIndex = 2;

        var colRem = _linesGridView.Columns.AddVisible(nameof(RefundLineItemRow.RemainingQuantity), "Eligible");
        colRem.OptionsColumn.AllowEdit = false;
        colRem.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
        colRem.DisplayFormat.FormatString = "N0";
        colRem.Width = 80;
        colRem.VisibleIndex = 3;

        var colRefundQty = _linesGridView.Columns.AddVisible(nameof(RefundLineItemRow.RefundQuantity), "Refund Qty *");
        var qtySpin = new RepositoryItemSpinEdit
        {
            MinValue = 0,
            MaxValue = 1000,
            IsFloatValue = false
        };
        _linesGrid.RepositoryItems.Add(qtySpin);
        colRefundQty.ColumnEdit = qtySpin;
        colRefundQty.Width = 90;
        colRefundQty.VisibleIndex = 4;

        var colPrice = _linesGridView.Columns.AddVisible(nameof(RefundLineItemRow.UnitPrice), "Price");
        colPrice.OptionsColumn.AllowEdit = false;
        colPrice.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Custom;
        colPrice.DisplayFormat.FormatString = "F2";
        colPrice.Width = 90;
        colPrice.VisibleIndex = 5;

        var colEst = _linesGridView.Columns.AddVisible(nameof(RefundLineItemRow.EstimatedRefund), "Est. Total");
        colEst.OptionsColumn.AllowEdit = false;
        colEst.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Custom;
        colEst.DisplayFormat.FormatString = "F2";
        colEst.Width = 90;
        colEst.VisibleIndex = 6;

        var colDisp = _linesGridView.Columns.AddVisible(nameof(RefundLineItemRow.Disposition), "Disposition");
        var dispCombo = new RepositoryItemComboBox();
        dispCombo.Items.AddRange(new object[] { InventoryDisposition.Restock, InventoryDisposition.DamagedDiscard, InventoryDisposition.NotApplicable });
        dispCombo.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        _linesGrid.RepositoryItems.Add(dispCombo);
        colDisp.ColumnEdit = dispCombo;
        colDisp.Width = 100;
        colDisp.VisibleIndex = 7;

        _linesGridView.CellValueChanged += (s, e) =>
        {
            if (e.Column.FieldName == nameof(RefundLineItemRow.RefundQuantity))
            {
                if (_linesGridView.GetRow(e.RowHandle) is RefundLineItemRow row)
                {
                    if (row.RefundQuantity > row.RemainingQuantity)
                    {
                        row.RefundQuantity = row.RemainingQuantity;
                        _linesGridView.SetRowCellValue(e.RowHandle, e.Column, row.RemainingQuantity);
                    }
                    else if (row.RefundQuantity < 0)
                    {
                        row.RefundQuantity = 0;
                        _linesGridView.SetRowCellValue(e.RowHandle, e.Column, 0);
                    }
                }
                UpdateSummary();
            }
        };

        // Populate settlement combo
        _settlementMethodCombo.Properties.Items.Clear();
        _settlementMethodCombo.Properties.Items.AddRange(new object[]
        {
            "Cash Payout",
            "External Card",
            "Customer Account Credit",
            "Merchandise Credit"
        });
        _settlementMethodCombo.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        _settlementMethodCombo.SelectedIndex = 0;

        _submitRefundButton.Click += async (s, e) => await SubmitRefundAsync();
    }

    private async Task LoadOrderLinesAsync()
    {
        try
        {
            var lines = await _mediator.Send(new ListOrderLinesByOrderQuery(_orderId));
            var existingRefunds = await _mediator.Send(new ListRefundsByOrderQuery(_orderId));
            var variants = await _mediator.Send(new ListProductVariantsQuery());
            var variantNames = variants.ToDictionary(v => v.ProductVariantId, v => v.Name);

            var refundedQtyByLineId = existingRefunds
                .SelectMany(r => r.Lines)
                .GroupBy(l => l.OrderLineId)
                .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));

            _lineRows.Clear();
            foreach (var line in lines)
            {
                var prevRefunded = refundedQtyByLineId.GetValueOrDefault(line.OrderLineId, 0m);
                var itemName = variantNames.GetValueOrDefault(line.ProductVariantId, line.Notes ?? "Menu Item");
                var row = new RefundLineItemRow
                {
                    OrderLineId = line.OrderLineId,
                    ProductVariantId = line.ProductVariantId,
                    ItemName = itemName,
                    OriginalQuantity = line.Quantity,
                    AlreadyRefundedQuantity = prevRefunded,
                    UnitPrice = line.UnitPrice,
                    RefundQuantity = 0,
                    Disposition = InventoryDisposition.Restock
                };
                _lineRows.Add(row);
            }

            UpdateSummary();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load order lines for refund on order {OrderId}", _orderId);
            XtraMessageBox.Show(this, $"Failed to load order details: {ex.Message}", "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void UpdateSummary()
    {
        var total = _lineRows.Sum(r => r.EstimatedRefund);
        var count = _lineRows.Count(r => r.RefundQuantity > 0);
        _summaryLabel.Text = $"Selected Items: {count}  |  Est. Net Refund: {CurrencyDisplay.FormatPlain(total)}";
        _submitRefundButton.Enabled = count > 0;
    }

    private async Task SubmitRefundAsync()
    {
        var itemsToRefund = _lineRows.Where(r => r.RefundQuantity > 0).ToList();
        if (itemsToRefund.Count == 0)
        {
            XtraMessageBox.Show(this, "Please enter a refund quantity greater than 0 for at least one item.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        foreach (var item in itemsToRefund)
        {
            if (item.RefundQuantity > item.RemainingQuantity)
            {
                XtraMessageBox.Show(this, $"Refund quantity for '{item.ItemName}' exceeds remaining eligible quantity ({item.RemainingQuantity}).", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

        var reason = _reasonEdit.Text.Trim();
        if (string.IsNullOrWhiteSpace(reason))
        {
            XtraMessageBox.Show(this, "Please provide a reason for the refund.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _reasonEdit.Focus();
            return;
        }

        var settlementMethod = _settlementMethodCombo.SelectedIndex switch
        {
            1 => RefundSettlementMethod.ExternalCardRefund,
            2 => RefundSettlementMethod.CustomerAccountCredit,
            3 => RefundSettlementMethod.MerchandiseCredit,
            _ => RefundSettlementMethod.CashPayout
        };

        var requests = itemsToRefund.Select(r => new RefundItemRequest(
            r.OrderLineId,
            r.RefundQuantity,
            r.Disposition)).ToList();

        var command = new ProcessRefundCommand(
            _orderId,
            requests,
            reason,
            settlementMethod,
            Guid.NewGuid().ToString("N"),
            _cashierId,
            _cashierName,
            BranchId: _branchId,
            ApprovingUserName: string.IsNullOrWhiteSpace(_approverEdit.Text) ? null : _approverEdit.Text.Trim(),
            SettlementReference: string.IsNullOrWhiteSpace(_settlementRefEdit.Text) ? null : _settlementRefEdit.Text.Trim());

        Cursor = Cursors.WaitCursor;
        _submitRefundButton.Enabled = false;

        try
        {
            var result = await _mediator.Send(command);
            ResultRefund = result;

            var printPrompt = XtraMessageBox.Show(
                this,
                $"Refund {result.RefundNumber} processed successfully.\n\nTotal Refunded: {CurrencyDisplay.FormatPlain(result.GrandTotalRefunded)}\n\nWould you like to preview/print the refund credit note?",
                "Refund Complete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);

            if (printPrompt == DialogResult.Yes)
            {
                var creditNoteText = ReceiptFormatter.FormatRefundCreditNote(result, _orderNumber);
                using var previewForm = new ReceiptPreviewForm(creditNoteText);
                previewForm.ShowDialog(this);
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to process refund for order {OrderId}", _orderId);
            XtraMessageBox.Show(this, $"Refund processing failed: {ex.Message}", "Refund Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
            _submitRefundButton.Enabled = true;
        }
    }
}
