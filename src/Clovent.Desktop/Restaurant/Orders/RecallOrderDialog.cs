using Clovent.Catalog.Application.Products.Dtos;
using Clovent.Catalog.Application.Products.Queries;
using Clovent.Catalog.Application.Variants.Dtos;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using Clovent.Restaurant.Application.ActivityLogs.Queries;
using Clovent.Restaurant.Application.Customers.Queries;
using Clovent.Restaurant.Application.OrderLines.Queries;
using Clovent.Restaurant.Application.Orders;
using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Application.Orders.Queries;
using Clovent.Restaurant.Application.Tables.Queries;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>Status tabs of the recall / sales history browser.</summary>
public enum RecallStatusFilter
{
    /// <summary>Orders on hold - the only status that can be recalled into the POS.</summary>
    Held,

    /// <summary>Currently open orders (mirrors the Active Orders rail).</summary>
    Open,

    /// <summary>Completed sales - historical, read-only.</summary>
    Closed,

    /// <summary>Voided and cancelled sales - historical, read-only, kept for audit.</summary>
    Voided,
}

/// <summary>
/// Operational order browser used by the POS Recall button: Held orders can
/// be recalled, Open orders can be opened, and Closed/Voided sales can be
/// inspected read-only. Reuses the existing order queries
/// (<see cref="ListHeldOrdersQuery"/>, <see cref="ListOpenOrdersQuery"/>,
/// <see cref="ListAllOrdersQuery"/>) rather than introducing a second
/// history architecture - the Order History screen queries the same
/// read models.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed partial class RecallOrderDialog : XtraForm
{
    private readonly IMediator _mediator;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _loadGate = new(1, 1);

    private RecallStatusFilter _status = RecallStatusFilter.Held;
    private List<OrderRow> _allRows = [];
    private bool _isLoading;
    private bool _suppressDateReload;

    /// <summary>The order chosen by the cashier (Held -&gt; recall, Open -&gt; open).</summary>
    public OrderDto? SelectedOrder { get; private set; }

    /// <summary>"Recall" when a Held order was chosen, "Open" for an Open order.</summary>
    public string SelectedAction { get; private set; } = "Recall";

    /// <summary>Design-time-only constructor - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public RecallOrderDialog()
    {
        _mediator = null!;
        _logger = null!;
        InitializeComponent();
    }

    /// <summary>Builds the recall dialog using the host screen's mediator and logger.</summary>
    public RecallOrderDialog(IMediator mediator, ILogger logger) : base()
    {
        InitializeComponent();

        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
        {
            _mediator = null!;
            _logger = null!;
            return;
        }

        _mediator = mediator;
        _logger = logger;
    }

    private void AppearanceManager_Changed(object? sender, EventArgs e)
    {
        // Re-apply palette accents if needed
    }

    private async void RecallOrderDialog_Load(object? sender, EventArgs e)
    {
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
            return;

        if (!_operationalSizeCustomized)
        {
            ApplyOperationalSize();
        }
        else
        {
            ApplyDpiScaling();
        }

        var screen = Owner != null ? Screen.FromControl(Owner) : (Screen.PrimaryScreen ?? Screen.FromPoint(new Point(0, 0)));
        _logger?.LogInformation(
            "RecallOrderDialog Runtime Bounds: Screen={ScreenW}x{ScreenH}, WorkingArea={WorkW}x{WorkH}, DPI={Dpi}, DialogSize={DlgW}x{DlgH}, ClientSize={ClientW}x{ClientH}",
            screen.Bounds.Width, screen.Bounds.Height,
            screen.WorkingArea.Width, screen.WorkingArea.Height,
            DeviceDpi,
            Width, Height,
            ClientSize.Width, ClientSize.Height);

        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string? dir = baseDir;
            string? qaDir = null;
            while (!string.IsNullOrEmpty(dir))
            {
                var candidate = Path.Combine(dir, "qa");
                if (Directory.Exists(candidate))
                {
                    qaDir = candidate;
                    break;
                }
                dir = Path.GetDirectoryName(dir);
            }

            if (!string.IsNullOrEmpty(qaDir))
            {
                var diagText = $@"=== RECALL ORDER DIALOG RUNTIME DIAGNOSTICS ===
Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}
Screen Bounds: {screen.Bounds.Width}x{screen.Bounds.Height}
Screen WorkingArea: {screen.WorkingArea.Width}x{screen.WorkingArea.Height}
Device DPI: {DeviceDpi}
DPI Scale Factor: {(DeviceDpi / 96.0):F2}x
Dialog Bounds: {Bounds}
Dialog Size: {Width}x{Height}
Dialog ClientSize: {ClientSize.Width}x{ClientSize.Height}
Dialog Location: {Location}
Dialog AutoScaleMode: {AutoScaleMode}
Dialog FormBorderStyle: {FormBorderStyle}
Dialog Font: {Font.Name} {Font.Size}pt
SearchBar Bounds: {(_searchBar != null ? _searchBar.Bounds.ToString() : "null")}
GridHost Bounds: {_gridHost.Bounds}
OrdersGridView RowHeight: {_ordersGridView.RowHeight}, ColumnPanelRowHeight: {_ordersGridView.ColumnPanelRowHeight}
PreviewPanel Bounds: {_previewPanel.Bounds}
PreviewItemsGrid Bounds: {_previewItemsGrid.Bounds}
FooterBar Bounds: {(_footerBar != null ? _footerBar.Bounds.ToString() : "null")}
ActionButton ('{_actionButton.Text}') Bounds: {_actionButton.Bounds}, Enabled: {_actionButton.Enabled}
CloseButton Bounds: {_closeButton.Bounds}
================================================
";
                File.AppendAllText(Path.Combine(qaDir, "recall_runtime_diagnostics.txt"), diagText);
            }
        }
        catch { /* best-effort diagnostic logging */ }

        // ApplyDatePreset ends with a reload, so this single call loads too.
        await ApplyDatePreset(DatePreset.Today);
    }

    private void RecallOrderDialog_Shown(object? sender, EventArgs e)
    {
        // Search is the fastest path to the right order, so it starts focused.
        _searchEdit.Focus();
    }

    private enum DatePreset { Today, Yesterday, ThisWeek, ThisMonth, All }

    private async Task ApplyDatePreset(DatePreset preset)
    {
        var businessNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, DateTimeDisplay.BusinessTimeZone);
        var today = businessNow.Date;
        // "All" is bounded only by the DateEdit-safe lower edge (1900 - the
        // editor can reject much older values), never by an upper bound.
        var from = preset switch
        {
            DatePreset.Yesterday => today.AddDays(-1),
            DatePreset.ThisWeek => today.AddDays(-(7 + (int)today.DayOfWeek - (int)DayOfWeek.Monday) % 7),
            DatePreset.ThisMonth => new DateTime(today.Year, today.Month, 1),
            DatePreset.All => new DateTime(1900, 1, 1),
            _ => today,
        };
        var to = preset switch
        {
            DatePreset.Yesterday => today.AddDays(-1),
            _ => today,
        };

        _suppressDateReload = true;
        _fromDateEdit.DateTime = from;
        _toDateEdit.DateTime = to;
        _suppressDateReload = false;

        // Presets own their reload: the per-keystroke reload above was
        // suppressed so setting both dates causes exactly one reload.
        await LoadAsync();
    }

    private void SwitchStatus(RecallStatusFilter status)
    {
        if (_status == status) return;
        _status = status;
        _ = LoadAsync();
    }

    /// <summary>
    /// Loads the current status's orders. Serialized through a gate so rapid
    /// tab clicks or Enter-hammering cannot stack duplicate queries.
    /// </summary>
    private async Task LoadAsync()
    {
        if (!await _loadGate.WaitAsync(0))
            return;

        try
        {
            _isLoading = true;
            SetBusy(true);
            UpdateTabStyles();

            IReadOnlyCollection<OrderDto> orders = _status switch
            {
                RecallStatusFilter.Held => await _mediator.Send(new ListHeldOrdersQuery()),
                RecallStatusFilter.Open => await _mediator.Send(new ListOpenOrdersQuery()),
                _ => (await _mediator.Send(new ListAllOrdersQuery()))
                    .Where(o => _status == RecallStatusFilter.Closed
                        ? o.Status == "Completed"
                        : o.Status is "Voided" or "Cancelled")
                    .ToList(),
            };

            // Non-historical statuses are live data; Closed/Voided are bounded
            // by the date filter (default Today) so history stays cheap.
            if (_status is RecallStatusFilter.Closed or RecallStatusFilter.Voided)
            {
                // A cleared DateEdit reports DateTime.MinValue - treat that as
                // "no bound on that side" rather than filtering everything out.
                var fromLocal = _fromDateEdit.DateTime == DateTime.MinValue
                    ? DateOnly.MinValue
                    : DateOnly.FromDateTime(_fromDateEdit.DateTime.Date);
                var toLocal = _toDateEdit.DateTime == DateTime.MinValue
                    ? DateOnly.MaxValue
                    : DateOnly.FromDateTime(_toDateEdit.DateTime.Date);
                orders = orders
                    .Where(o =>
                    {
                        var local = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(o.UpdatedAtUtc, DateTimeDisplay.BusinessTimeZone).Date);
                        return local >= fromLocal && local <= toLocal;
                    })
                    .ToList();
            }

            orders = orders
                .Where(o => o.OrderLineIds.Count > 0)
                .OrderByDescending(o => o.UpdatedAtUtc)
                .ToList();

            var tables = await _mediator.Send(new ListAllTablesQuery());
            var tableCodes = tables.ToDictionary(t => t.TableId, t => t.Code);

            var customers = await _mediator.Send(new ListCustomersQuery());
            var customersById = customers.ToDictionary(c => c.CustomerId);

            // Void/cancel reasons and who held an order live in the activity
            // log - the same audit trail the Activity Log screen reads.
            var activity = _status is RecallStatusFilter.Held or RecallStatusFilter.Voided
                ? await _mediator.Send(new ListRecentActivityQuery(500))
                : [];

            var rows = new List<OrderRow>(orders.Count);
            foreach (var order in orders)
            {
                var summary = await _mediator.Send(new GetOrderSummaryQuery(order.OrderId));
                rows.Add(ToRow(order, summary, tableCodes, customersById, activity));
            }

            _allRows = rows;
            ApplyFilter();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load {Status} orders for the recall dialog", _status);
            XtraMessageBox.Show(this, "Unable to load orders. Please try again.", "Recall / Sales History", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _isLoading = false;
            SetBusy(false);
            _loadGate.Release();
        }
    }

    private OrderRow ToRow(
        OrderDto order,
        OrderTotals summary,
        IReadOnlyDictionary<Guid, string> tableCodes,
        IReadOnlyDictionary<Guid, Clovent.Restaurant.Application.Customers.Dtos.CustomerDto> customersById,
        IReadOnlyCollection<Clovent.Restaurant.Application.ActivityLogs.Dtos.ActivityLogEntryDto> activity)
    {
        var orderTypeDisplay = order.OrderType == "TakeAway" ? "Take Away" : "Dine In";

        // Section 18: Table display must never duplicate prefixes (no T-T-01)
        var tableDisplay = "-";
        if (order.TableId is { } tid && tableCodes.TryGetValue(tid, out var rawCode) && !string.IsNullOrWhiteSpace(rawCode))
        {
            var code = rawCode.Trim();
            tableDisplay = code.StartsWith("T-", StringComparison.OrdinalIgnoreCase)
                ? code
                : code.StartsWith("Table", StringComparison.OrdinalIgnoreCase)
                    ? code
                    : $"T-{code}";
        }

        var customer = order.CustomerId is { } cid && customersById.TryGetValue(cid, out var c) ? c : null;
        var customerName = customer?.Name ?? "Walk-in Customer";
        var phone = customer?.MobileNumber is { Length: > 0 } mobile ? mobile : "-";
        var invoiceDisplay = order.DailySalesNumber is { } n ? $"INV-{n:D6}" : "-";

        var paymentDisplay = summary.Balance <= 0.005m
            ? (summary.PaidTotal > 0 ? "Paid" : "Unpaid")
            : summary.PaidTotal > 0 ? "Partial" : "Unpaid";

        var statusDisplay = order.Status switch
        {
            "Completed" => "Closed",
            "Cancelled" => "Cancelled",
            _ => order.Status,
        };

        var related = activity
            .Where(a => a.Details?.Contains(order.OrderNumber, StringComparison.OrdinalIgnoreCase) == true)
            .OrderByDescending(a => a.OccurredAtUtc)
            .ToList();
        var reasonEntry = related.FirstOrDefault(a =>
            a.Action.Equals("Void", StringComparison.OrdinalIgnoreCase) ||
            a.Action.Equals("Cancel Order", StringComparison.OrdinalIgnoreCase));
        var heldEntry = related.FirstOrDefault(a => a.Action.Equals("Hold Order", StringComparison.OrdinalIgnoreCase));

        var heldBy = heldEntry?.PerformedBy ?? (order.Status == "Held" ? "Cashier" : "-");
        var performedBy = order.Status == "Held" ? heldBy : (reasonEntry?.PerformedBy ?? "-");

        var searchText = string.Join(' ',
            order.OrderNumber,
            invoiceDisplay,
            order.DailySalesNumber?.ToString() ?? "",
            orderTypeDisplay,
            tableDisplay,
            customerName,
            phone,
            statusDisplay,
            performedBy,
            order.Notes ?? "").ToLowerInvariant();

        return new OrderRow(
            order.OrderId,
            order.OrderNumber,
            invoiceDisplay,
            orderTypeDisplay,
            tableDisplay,
            customerName,
            phone,
            order.OrderLineIds.Count,
            CurrencyDisplay.FormatPlain(summary.GrandTotal),
            paymentDisplay,
            DateTimeDisplay.Format(order.UpdatedAtUtc),
            _status == RecallStatusFilter.Held ? FormatRelativeAge(order.UpdatedAtUtc) : "-",
            reasonEntry?.Details is { } rd ? ExtractReason(rd) : "-",
            performedBy,
            statusDisplay,
            searchText,
            order);
    }

    private static string ExtractReason(string details)
    {
        // Details look like "Voided order ORD-9. Reason: wrong order." /
        // "Cancelled order ORD-9. Reason: customer changed mind."
        var marker = "reason:";
        var index = details.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        return index < 0 ? details : details[(index + marker.Length)..].Trim().TrimEnd('.');
    }

    private void ApplyFilter()
    {
        var filter = _searchEdit.Text?.Trim().ToLowerInvariant();
        var filtered = string.IsNullOrEmpty(filter)
            ? _allRows
            : _allRows.Where(r => r.SearchText.Contains(filter)).ToList();

        _ordersGrid.DataSource = filtered;

        _countLabel.Text = filtered.Count == 1 ? "1 order found" : $"{filtered.Count} orders found";

        _emptyStateLabel.Text = _allRows.Count == 0
            ? _status switch
            {
                RecallStatusFilter.Held => "No held orders found.",
                RecallStatusFilter.Open => "No open orders found.",
                RecallStatusFilter.Closed => "No closed sales found for the selected period.",
                _ => "No voided sales found for the selected period.",
            }
            : "No orders match your search.";
        _emptyStateLabel.Visible = filtered.Count == 0;
        if (filtered.Count == 0)
        {
            _emptyStateLabel.BringToFront();
        }
        else
        {
            _ordersGrid.BringToFront();
        }

        // A single result can be preselected (convenience, not accidental
        // recall - the cashier still presses Recall / Enter).
        if (filtered.Count == 1)
            _ordersGridView.FocusedRowHandle = 0;

        UpdateColumnVisibility();
        UpdateContentLayout();
        OnSelectionChanged();
    }

    private void UpdateColumnVisibility()
    {
        ColumnByField("AgeDisplay").Visible = false;
        ColumnByField("PerformedBy").Visible = _status == RecallStatusFilter.Held;
        ColumnByField("Reason").Visible = _status == RecallStatusFilter.Voided;
        ColumnByField("InvoiceDisplay").Visible = _status is RecallStatusFilter.Closed or RecallStatusFilter.Voided;
        ColumnByField("DateTimeDisplay").Visible = true;
        ColumnByField("PaymentDisplay").Visible = _status == RecallStatusFilter.Closed;
        ColumnByField("Phone").Visible = true;

        ColumnByField("OrderNumber").Visible = true;
        ColumnByField("TypeDisplay").Visible = true;
        ColumnByField("TableDisplay").Visible = true;
        ColumnByField("CustomerName").Visible = true;
        ColumnByField("ItemCount").Visible = true;
        ColumnByField("TotalDisplay").Visible = true;
        ColumnByField("StatusDisplay").Visible = true;
    }

    private DevExpress.XtraGrid.Columns.GridColumn ColumnByField(string field) =>
        _ordersGridView.Columns[field] ?? throw new InvalidOperationException($"Missing column {field}");

    private void UpdateTabStyles()
    {
        StyleTab(_heldTabButton, _status == RecallStatusFilter.Held, Color.FromArgb(13, 148, 136));
        StyleTab(_openTabButton, _status == RecallStatusFilter.Open, Color.FromArgb(37, 99, 235));
        StyleTab(_closedTabButton, _status == RecallStatusFilter.Closed, Color.FromArgb(71, 85, 105));
        StyleTab(_voidedTabButton, _status == RecallStatusFilter.Voided, Color.FromArgb(220, 38, 38));

        // The date filter only bounds historical statuses (Closed / Voided).
        var showDates = _status is RecallStatusFilter.Closed or RecallStatusFilter.Voided;
        if (_dateBar != null)
        {
            _dateBar.Visible = showDates;
            if (_dateBar.Parent is TableLayoutPanel top)
            {
                var rowIndex = top.GetRow(_dateBar);
                if (rowIndex >= 0 && rowIndex < top.RowStyles.Count)
                {
                    top.RowStyles[rowIndex].SizeType = SizeType.AutoSize;
                }
            }
        }
        else if (_fromDateEdit.Parent != null)
        {
            _fromDateEdit.Parent.Visible = showDates;
        }
    }

    private static void StyleTab(SimpleButton button, bool selected, Color accent)
    {
        button.Appearance.BackColor = selected ? accent : Color.White;
        button.Appearance.ForeColor = selected ? Color.White : Color.FromArgb(71, 85, 105);
        button.Appearance.BorderColor = selected ? accent : Color.FromArgb(203, 213, 225);
        button.Appearance.Options.UseBackColor = true;
        button.Appearance.Options.UseForeColor = true;
        button.Appearance.Options.UseBorderColor = true;
    }

    private void SetBusy(bool busy)
    {
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
        _refreshButton.Enabled = !busy;
        _actionButton.Enabled = !busy && GetFocusedRow() is not null;
        foreach (var button in new[] { _heldTabButton, _openTabButton, _closedTabButton, _voidedTabButton })
            button.Enabled = !busy;
    }

    private void OnSelectionChanged()
    {
        var row = GetFocusedRow();
        _actionButton.Text = _status switch
        {
            RecallStatusFilter.Held => "Recall Order",
            RecallStatusFilter.Open => "Open Order",
            _ => "View",
        };
        _actionButton.Enabled = row is not null;

        if (row is null)
        {
            _previewHeaderLabel.Text = "Select an order to see its details.";
            _previewSummaryLabel.Text = "";
            _previewMetaLabel.Text = "";
            _previewLinesLabel.Text = "";
            _previewItemsGrid.DataSource = null;
            _previewItemsGrid.Visible = false;
            return;
        }

        var tableText = row.TableDisplay == "-" ? "None" : row.TableDisplay;
        var phoneText = row.Phone == "-" ? "" : $" ({row.Phone})";
        _previewHeaderLabel.Text = $"Selected:  {row.OrderNumber}  •  {row.TypeDisplay}  •  {row.StatusDisplay}";

        _previewSummaryLabel.Text =
            $"Customer: {row.CustomerName}{phoneText}    •    Table: {tableText}    •    " +
            $"Items: {row.ItemCount}    •    Total: Rs. {row.TotalDisplay}";

        var heldByText = !string.IsNullOrEmpty(row.PerformedBy) && row.PerformedBy != "-" ? row.PerformedBy : "Administrator";
        var heldAge = row.AgeDisplay != "-" ? row.AgeDisplay : "Recent";
        var heldInfo = _status == RecallStatusFilter.Held
            ? (row.AgeDisplay != "-" ? $"Held: {heldAge} by {heldByText}" : $"Held by {heldByText}")
            : (_status == RecallStatusFilter.Voided && row.Reason != "-" ? $"Reason: {row.Reason}" : $"Status: {row.StatusDisplay}");

        _previewMetaLabel.Text = $"Payment: {row.PaymentDisplay}    •    {heldInfo}";

        _ = LoadPreviewAsync(row);
    }

    private async Task LoadPreviewAsync(OrderRow row)
    {
        var requested = row;
        try
        {
            var lines = (await _mediator.Send(new ListOrderLinesByOrderQuery(row.OrderId)))
                .Where(l => !l.IsVoided)
                .ToList();

            // A slow load must never populate the preview of a row the
            // cashier has since moved away from.
            if (!ReferenceEquals(GetFocusedRow(), requested))
                return;

            var previewItems = new List<PreviewItemRow>();
            var text = new System.Text.StringBuilder();

            foreach (var line in lines)
            {
                ProductVariantDto? variant = null;
                try { variant = await _mediator.Send(new GetProductVariantByIdQuery(line.ProductVariantId)); }
                catch { }

                ProductDto? product = null;
                if (variant != null)
                {
                    try { product = await _mediator.Send(new GetProductByIdQuery(variant.ProductId)); }
                    catch { }
                }

                string itemName = product?.Name ?? variant?.Name ?? "(item)";
                string variantName = (product != null && variant != null && !string.Equals(variant.Name, product.Name, StringComparison.OrdinalIgnoreCase))
                    ? variant.Name
                    : (variant?.Name ?? "Standard");

                var itemRow = new PreviewItemRow
                {
                    Item = itemName,
                    Variant = variantName,
                    Qty = (int)Math.Round(line.Quantity),
                    Price = CurrencyDisplay.FormatPlain(line.UnitPrice),
                    Amount = CurrencyDisplay.FormatPlain(line.LineTotal)
                };
                previewItems.Add(itemRow);
                text.AppendLine($"{itemName,-25} {variantName,-12} {itemRow.Qty,3} × {itemRow.Price,10}  =  {itemRow.Amount,10}");
            }

            if (!ReferenceEquals(GetFocusedRow(), requested))
                return;

            _previewLinesLabel.Text = text.ToString();
            _previewItemsGrid.DataSource = previewItems;
            _previewItemsGrid.Visible = previewItems.Count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load preview for order {OrderId}", row.OrderId);
        }
    }

    private OrderRow? GetFocusedRow() => _ordersGridView.GetFocusedRow() as OrderRow;

    /// <summary>
    /// The primary action for the selected status. Only Held orders can be
    /// recalled; Open orders are opened; Closed/Voided sales are shown
    /// read-only and can never re-enter the POS as editable carts.
    /// </summary>
    private async void RunPrimaryAction()
    {
        var row = GetFocusedRow();
        if (row is null || _isLoading) return;

        // Stale-data guard: another session may have moved the order on.
        OrderDto? fresh = null;
        try
        {
            fresh = await _mediator.Send(new GetOrderByIdQuery(row.OrderId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to re-read order {OrderId} before recall", row.OrderId);
        }

        if (_status == RecallStatusFilter.Held)
        {
            if (fresh is null || fresh.Status != "Held")
            {
                XtraMessageBox.Show(this,
                    "This order is no longer available for recall. Refresh the list.",
                    "Order Unavailable", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                await LoadAsync();
                return;
            }

            SelectedOrder = fresh;
            SelectedAction = "Recall";
            DialogResult = DialogResult.OK;
            Close();
            return;
        }

        if (_status == RecallStatusFilter.Open)
        {
            if (fresh is null || fresh.Status != "Open")
            {
                XtraMessageBox.Show(this,
                    "This order is no longer open. Refresh the list.",
                    "Order Unavailable", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                await LoadAsync();
                return;
            }

            SelectedOrder = fresh;
            SelectedAction = "Open";
            DialogResult = DialogResult.OK;
            Close();
            return;
        }

        // Closed / Voided: read-only inspection, no mutation, no recall.
        await ShowReadOnlyDetailsAsync(row);
    }

    /// <summary>
    /// Shows the historical order in a read-only viewer. Must stay async all
    /// the way down - blocking on the details query with GetAwaiter().GetResult()
    /// deadlocks the UI thread against the dialog's own message pump.
    /// </summary>
    private async Task ShowReadOnlyDetailsAsync(OrderRow row)
    {
        try
        {
            var details = await BuildReadOnlyDetailsAsync(row);
            using var viewer = new XtraForm
            {
                Text = $"Order {row.OrderNumber} — {row.StatusDisplay} (read-only)",
                StartPosition = FormStartPosition.CenterParent,
                Size = new Size(520, 480),
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false,
                FormBorderStyle = FormBorderStyle.SizableToolWindow
            };
            var memo = new DevExpress.XtraEditors.MemoEdit
            {
                Dock = DockStyle.Fill,
                Text = details,
                Properties = { ReadOnly = true }
            };
            memo.Properties.Appearance.Font = new Font("Consolas", 9.5F);
            viewer.Controls.Add(memo);
            viewer.ShowDialog(this);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show read-only details for order {OrderId}", row.OrderId);
            XtraMessageBox.Show(this, "Unable to load the order details. Please try again.", "Order Details", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task<string> BuildReadOnlyDetailsAsync(OrderRow row)
    {
        var lines = (await _mediator.Send(new ListOrderLinesByOrderQuery(row.OrderId))).ToList();
        var summary = await _mediator.Send(new GetOrderSummaryQuery(row.OrderId));
        var payments = await _mediator.Send(new Clovent.Restaurant.Application.Payments.Queries.ListPaymentsByOrderQuery(row.OrderId));

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Order:    {row.OrderNumber}   ({row.StatusDisplay})");
        sb.AppendLine($"Invoice:  {row.InvoiceDisplay}");
        sb.AppendLine($"Type:     {row.TypeDisplay}   Table: {row.TableDisplay}");
        sb.AppendLine($"Customer: {row.CustomerName}   {row.Phone}");
        sb.AppendLine($"Updated:  {row.DateTimeDisplay}");
        if (row.Reason != "-")
            sb.AppendLine($"Reason:   {row.Reason}");
        sb.AppendLine(new string('-', 48));
        foreach (var line in lines)
        {
            string name;
            try { name = (await _mediator.Send(new GetProductVariantByIdQuery(line.ProductVariantId))).Name; }
            catch { name = "(item)"; }
            sb.AppendLine($"{(line.IsVoided ? "[voided] " : "")}{name,-28} {line.Quantity:N0} × {CurrencyDisplay.FormatPlain(line.UnitPrice),10} = {CurrencyDisplay.FormatPlain(line.LineTotal),10}");
            if (line.Notes is { } notes) sb.AppendLine($"    Note: {notes}");
        }
        sb.AppendLine(new string('-', 48));
        sb.AppendLine($"{"Subtotal",-38}{CurrencyDisplay.FormatPlain(summary.Subtotal),12}");
        if (summary.DiscountTotal != 0) sb.AppendLine($"{"Discount",-38}{CurrencyDisplay.FormatPlain(-summary.DiscountTotal),12}");
        if (summary.TaxTotal != 0) sb.AppendLine($"{"Tax",-38}{CurrencyDisplay.FormatPlain(summary.TaxTotal),12}");
        if (summary.ServiceChargeTotal != 0) sb.AppendLine($"{"Service Charge",-38}{CurrencyDisplay.FormatPlain(summary.ServiceChargeTotal),12}");
        sb.AppendLine($"{"TOTAL",-38}{CurrencyDisplay.FormatPlain(summary.GrandTotal),12}");
        sb.AppendLine($"{"Paid",-38}{CurrencyDisplay.FormatPlain(summary.PaidTotal),12}");
        if (payments.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Payments:");
            foreach (var payment in payments)
                sb.AppendLine($"  {DateTimeDisplay.Format(payment.CreatedAtUtc)}   {CurrencyDisplay.FormatPlain(payment.Amount),12}{(payment.IsVoided ? "   (voided)" : "")}");
        }
        sb.AppendLine();
        sb.AppendLine("This order is historical and read-only.");
        return sb.ToString();
    }

    private static string FormatRelativeAge(DateTimeOffset dt)
    {
        var span = DateTimeOffset.UtcNow - dt;
        return span.TotalMinutes < 1 ? "just now"
            : span.TotalMinutes < 60 ? $"{(int)span.TotalMinutes} min ago"
            : span.TotalHours < 24 ? $"{(int)span.TotalHours} hr ago"
            : $"{(int)span.TotalDays} d ago";
    }

    /// <summary>One order as listed by the browser, with preformatted display values.</summary>
    private sealed record OrderRow(
        Guid OrderId,
        string OrderNumber,
        string InvoiceDisplay,
        string TypeDisplay,
        string TableDisplay,
        string CustomerName,
        string Phone,
        int ItemCount,
        string TotalDisplay,
        string PaymentDisplay,
        string DateTimeDisplay,
        string AgeDisplay,
        string Reason,
        string PerformedBy,
        string StatusDisplay,
        string SearchText,
        OrderDto Order);

    /// <summary>Tabular row model for line items in the Selected Order Details grid.</summary>
    public sealed class PreviewItemRow
    {
        public string Item { get; set; } = "";
        public string Variant { get; set; } = "";
        public int Qty { get; set; }
        public string Price { get; set; } = "";
        public string Amount { get; set; } = "";
    }
}
