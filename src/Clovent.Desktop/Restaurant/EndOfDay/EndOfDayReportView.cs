using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Catalog.Application.Products.Queries;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using Clovent.Desktop.Restaurant.Orders;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Inventory.Application.Transactions.Dtos;
using Clovent.Inventory.Application.Transactions.Queries;
using Clovent.Inventory.Application.WarehouseStocks.Dtos;
using Clovent.Inventory.Application.WarehouseStocks.Queries;
using Clovent.MasterData.Application.Currencies.Queries;
using Clovent.MasterData.Application.Warehouses.Queries;
using Clovent.Restaurant.Application.EndOfDay.Dtos;
using Clovent.Restaurant.Application.EndOfDay.Queries;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Restaurant.EndOfDay;

/// <summary>
/// Sales Summary (Day-End / Z-Report): Comprehensive sales overview across 11 tabs:
/// Summary KPIs, Orders/Bills (master-detail lines + preview), Items (with margins &amp; food cost),
/// Customers, Payments (tenders &amp; AR), Receivables Movement, Order Types, Item Types/Profitability,
/// Cash Summary, Inventory Movement, and Stock Remaining.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed partial class EndOfDayReportView : XtraUserControl
{
    private const string FeatureCode = "endofday";

    private readonly IServiceScope _scope;
    private readonly ScreenOperationGate _gate = new();
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;

    private Dictionary<Guid, (string Sku, string VariantName, string ProductName)> _variantsById = [];
    private string _summaryText = string.Empty;
    private bool _isUpdatingPeriod;

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public EndOfDayReportView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = new SerializedMediator(_scope.ServiceProvider.GetRequiredService<IMediator>(), _gate);
        _featurePolicy = new SerializedFeatureAuthorizationPolicy(_scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>(), _gate);
        _currentSession = currentSession;

        AppearanceManager.Changed += AppearanceManager_Changed;

        InitializeComponent();
        ConfigureReportPeriodOptions();
    }

    private void ConfigureReportPeriodOptions()
    {
        if (DesignModeHelper.IsInDesignMode) return;
        _periodCombo.Properties.Items.Clear();
        foreach (var (_, name) in ReportPeriodCalculator.GetAllOptions())
        {
            _periodCombo.Properties.Items.Add(name);
        }
        _periodCombo.SelectedIndex = -1;
        _periodCombo.SelectedItem = "Today";
    }

    private void AppearanceManager_Changed(object? sender, EventArgs e) => AppearanceManager.Apply(this, "Restaurant", nameof(EndOfDayReportView));

    private void PeriodCombo_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_isUpdatingPeriod) return;

        var periodName = _periodCombo.SelectedItem?.ToString();
        var period = ReportPeriodCalculator.ParseDisplayName(periodName);
        if (period == ReportPeriod.Custom) return;

        _isUpdatingPeriod = true;
        try
        {
            var range = ReportPeriodCalculator.CalculateRange(period, BusinessDateTimeService.Instance.Today);
            _fromDateEdit.EditValue = range.From.ToDateTime(TimeOnly.MinValue);
            _toDateEdit.EditValue = range.To.ToDateTime(TimeOnly.MinValue);
        }
        finally
        {
            _isUpdatingPeriod = false;
        }
    }

    private void DateEdit_EditValueChanged(object? sender, EventArgs e)
    {
        if (_isUpdatingPeriod) return;

        if (_periodCombo.SelectedItem?.ToString() != "Custom")
        {
            _isUpdatingPeriod = true;
            try
            {
                _periodCombo.SelectedItem = "Custom";
            }
            finally
            {
                _isUpdatingPeriod = false;
            }
        }
    }

    private async void GenerateButton_Click(object? sender, EventArgs e) => await GenerateAsync();

    private void PrintSummaryButton_Click(object? sender, EventArgs e) => PrintSummary();

    private void TabControl_SelectedPageChanged(object? sender, DevExpress.XtraTab.TabPageChangedEventArgs e) =>
        UpdateActionEnablement();

    private void UpdateActionEnablement()
    {
        bool hasReport = !string.IsNullOrEmpty(_summaryText);
        _printSummaryButton.Enabled = hasReport;

        if (_tabControl == null) return;

        if (_tabControl.SelectedTabPageIndex == 0)
        {
            _previewButton.Enabled = hasReport;
            _printButton.Enabled = hasReport;
            _exportPdfButton.Enabled = false;
            _exportExcelButton.Enabled = false;
        }
        else
        {
            var grid = GetCurrentTabGrid();
            var view = grid?.MainView as DevExpress.XtraGrid.Views.Grid.GridView;
            bool hasRows = view != null && view.RowCount > 0;
            _previewButton.Enabled = hasRows;
            _printButton.Enabled = hasRows;
            _exportPdfButton.Enabled = hasRows;
            _exportExcelButton.Enabled = hasRows;
        }
    }

    private GridControl? GetCurrentTabGrid() => _tabControl.SelectedTabPageIndex switch
    {
        1 => _ordersGrid,
        2 => _itemsGrid,
        3 => _customersGrid,
        4 => _paymentsGrid,
        5 => _receivablesGrid,
        6 => _orderTypesGrid,
        7 => _itemTypesGrid,
        8 => _cashSummaryGrid,
        9 => _inventoryMovementGrid,
        10 => _stockRemainingGrid,
        _ => null
    };

    private static readonly string[] PrintableOrderColumns =
    [
        "OrderNumber", "OrderType", "CustomerName", "ItemsCount",
        "Subtotal", "Discount", "Tax", "Total", "PaidAmount",
        "OnAccountAmount", "PaymentSummary", "Status", "CreatedAtUtc"
    ];

    private string GetCurrentTabReportTitle() => _tabControl.SelectedTabPageIndex switch
    {
        1 => "ORDERS & BILLS REGISTER",
        2 => "ITEM SALES PERFORMANCE",
        3 => "CUSTOMER SALES & RECEIVABLES",
        4 => "PAYMENT TENDER SUMMARY",
        5 => "RECEIVABLES MOVEMENT & AGING",
        6 => "ORDER TYPE PERFORMANCE BREAKDOWN",
        7 => "ITEM CLASSIFICATION & PROFITABILITY",
        8 => "CASH SUMMARY",
        9 => "INVENTORY MOVEMENT",
        10 => "STOCK REMAINING",
        _ => "SALES SUMMARY"
    };

    private string GetDateRangeSubtitle()
    {
        var from = _fromDateEdit.EditValue is DateTime f ? f : BusinessDateTimeService.Instance.Today.ToDateTime(TimeOnly.MinValue);
        var to = _toDateEdit.EditValue is DateTime t ? t : BusinessDateTimeService.Instance.Today.ToDateTime(TimeOnly.MinValue);
        return $"Period: {from:dd-MMM-yyyy} to {to:dd-MMM-yyyy}";
    }

    private string GetCurrentTabExportName() => _tabControl.SelectedTabPageIndex switch
    {
        1 => "OrdersBills",
        2 => "Items",
        3 => "Customers",
        4 => "Payments",
        5 => "ReceivablesMovement",
        6 => "OrderTypes",
        7 => "ItemProfitability",
        8 => "CashSummary",
        9 => "InventoryMovement",
        10 => "StockRemaining",
        _ => "Report"
    };

    private void PreviewButton_Click(object? sender, EventArgs e)
    {
        if (_tabControl.SelectedTabPageIndex == 0)
        {
            PrintSummary();
            return;
        }

        var grid = GetCurrentTabGrid();
        if (grid == null) return;

        var title = GetCurrentTabReportTitle();
        var subtitle = GetDateRangeSubtitle();

        if (grid == _ordersGrid)
        {
            GridReportingPrintService.ShowPreview(grid, title, subtitle, this, landscape: true, visiblePrintColumns: PrintableOrderColumns);
        }
        else
        {
            GridReportingPrintService.ShowPreview(grid, title, subtitle, this, landscape: true);
        }
    }

    private void PrintButton_Click(object? sender, EventArgs e)
    {
        if (_tabControl.SelectedTabPageIndex == 0)
        {
            PrintSummary();
            return;
        }

        var grid = GetCurrentTabGrid();
        if (grid == null) return;

        var title = GetCurrentTabReportTitle();
        var subtitle = GetDateRangeSubtitle();

        if (grid == _ordersGrid)
        {
            GridReportingPrintService.Print(grid, title, subtitle, this, landscape: true, visiblePrintColumns: PrintableOrderColumns);
        }
        else
        {
            GridReportingPrintService.Print(grid, title, subtitle, this, landscape: true);
        }
    }

    private void ExportPdfButton_Click(object? sender, EventArgs e)
    {
        var grid = GetCurrentTabGrid();
        if (grid == null) return;

        var title = GetCurrentTabReportTitle();
        var subtitle = GetDateRangeSubtitle();

        if (grid == _ordersGrid)
        {
            GridReportingPrintService.ExportToPdf(grid, title, subtitle, this, landscape: true, visiblePrintColumns: PrintableOrderColumns);
        }
        else
        {
            GridReportingPrintService.ExportToPdf(grid, title, subtitle, this, landscape: true);
        }
    }

    private void ExportExcelButton_Click(object? sender, EventArgs e)
    {
        var grid = GetCurrentTabGrid();
        if (grid == null) return;
        var name = GetCurrentTabExportName();
        var options = new DevExpress.XtraPrinting.XlsxExportOptionsEx
        {
            ExportType = DevExpress.Export.ExportType.WYSIWYG
        };
        ExportGrid(grid, "Excel files (*.xlsx)|*.xlsx", $"{name}.xlsx", (g, path) => g.ExportToXlsx(path, options));
    }

    private async void EndOfDayReportView_Load(object? sender, EventArgs e)
    {
        ScaleLayoutAtRuntime();
        DpiChangedAfterParent += (_, _) => ScaleLayoutAtRuntime();
        UpdateActionEnablement();
        AppearanceManager.Apply(this, "Restaurant", nameof(EndOfDayReportView));
        await LoadAndShowTodayAsync();
    }

    /// <inheritdoc/>
    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        ScaleLayoutAtRuntime();
    }

    private async Task LoadAndShowTodayAsync()
    {
        await LoadWarehousesAsync();

        if (_warehousePicker.SelectedId is not null)
        {
            var today = BusinessDateTimeService.Instance.Today.ToDateTime(TimeOnly.MinValue);
            await SetDateRangeAndGenerateAsync(today, today);
        }
    }

    private async Task SetDateRangeAndGenerateAsync(DateTime from, DateTime to)
    {
        _isUpdatingPeriod = true;
        try
        {
            _fromDateEdit.EditValue = from;
            _toDateEdit.EditValue = to;
        }
        finally
        {
            _isUpdatingPeriod = false;
        }
        await GenerateAsync();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
            AppearanceManager.Changed -= AppearanceManager_Changed;
            _scope.Dispose();
            _gate.Dispose();
        }

        base.Dispose(disposing);
    }

    private async Task LoadWarehousesAsync()
    {
        var warehouses = await _mediator.Send(new ListAllWarehousesQuery());
        if (warehouses != null)
        {
            _warehousePicker.LoadItems([.. warehouses.Select(w => (w.WarehouseId, w.Name))]);
            _warehousePicker.Visible = warehouses.Count > 1;
        }
    }

    private async Task GenerateAsync()
    {
        if (_warehousePicker.SelectedId is not { } warehouseId)
        {
            XtraMessageBox.Show(this, "Select a location first.", "No Location Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var fromDate = DateOnly.FromDateTime((DateTime)_fromDateEdit.EditValue);
        var toDate = DateOnly.FromDateTime((DateTime)_toDateEdit.EditValue);
        if (toDate < fromDate)
        {
            XtraMessageBox.Show(this, "'To' date cannot be before 'From' date.", "Invalid Date Range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        UseWaitCursor = true;
        try
        {
            await GenerateCoreAsync(warehouseId, fromDate, toDate);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private async Task GenerateCoreAsync(Guid warehouseId, DateOnly fromDate, DateOnly toDate)
    {
        await CurrencyDisplayLoader.ConfigureAsync(_mediator);

        var variants = await _mediator.Send(new ListProductVariantsQuery());
        var products = await _mediator.Send(new ListProductsQuery());
        var productNameByProductId = products.ToDictionary(p => p.ProductId, p => p.Name);
        _variantsById = variants.ToDictionary(
            v => v.ProductVariantId,
            v => (
                v.Sku,
                v.Name,
                productNameByProductId.GetValueOrDefault(v.ProductId, string.Empty)
            ));

        ExpandedSalesSummaryDto? expanded = null;
        try
        {
            expanded = await _mediator.Send(new GetExpandedSalesSummaryQuery(warehouseId, fromDate, toDate));
        }
        catch { }

        var report = await _mediator.Send(new GetEndOfDayReportQuery(warehouseId, fromDate, toDate));

        _totalBillsValueLabel.Text = (expanded?.Kpis.TotalOrders ?? report.ReceiptCount).ToString();
        _totalSalesValueLabel.Text = CurrencyDisplay.Format(expanded?.Kpis.TotalBillSalesValue ?? report.TotalSales);
        _cashValueLabel.Text = CurrencyDisplay.Format(expanded?.Kpis.CashCollected ?? report.CashCollected);
        _cardValueLabel.Text = CurrencyDisplay.Format(expanded?.Kpis.CardCollected ?? report.CardCollected);
        _voidedCountLabel.Text = $"{expanded?.Kpis.VoidedOrdersCount ?? report.VoidedOrderCount}";
        _averageSaleLabel.Text = CurrencyDisplay.Format(expanded?.Kpis.AverageOrderValue ?? report.AverageSale);
        _summaryText = BuildSummaryText(report, expanded, fromDate, toDate);

        _summaryEmptyStateLabel.Visible = (expanded?.Kpis.TotalOrders ?? report.ReceiptCount) == 0;

        // 1. Orders / Bills
        if (expanded != null && expanded.Orders.Count > 0)
        {
            _ordersGrid.DataSource = expanded.Orders;
        }
        else
        {
            _ordersGrid.DataSource = report.Bills.Select(b => new ExpandedOrderRowDto(
                Guid.Empty,
                b.OrderNumber,
                null,
                "DineIn",
                "WalkIn",
                "-",
                "Guest",
                1,
                b.Total,
                0m,
                0m,
                0m,
                b.Total,
                "Completed",
                b.CompletedAtUtc,
                b.PaymentMethodSummary,
                b.Total,
                0m,
                0m,
                null)).ToList();
        }

        // 2. Items
        if (expanded != null && expanded.Items.Count > 0)
        {
            _itemsGrid.DataSource = expanded.Items;
        }
        else
        {
            _itemsGrid.DataSource = report.ItemsSold.Select(i => new ExpandedItemRowDto(
                i.ProductVariantId,
                "-",
                ResolveName(i.ProductVariantId),
                "Prepared",
                i.Quantity,
                i.Quantity > 0 ? Math.Round(i.Total / i.Quantity, 2) : 0m,
                null,
                i.Total,
                null,
                null,
                null,
                report.TotalSales > 0 ? Math.Round(i.Total / report.TotalSales * 100m, 2) : 0m)).ToList();
        }

        // 3. Customers
        if (expanded != null && expanded.Customers.Count > 0)
        {
            _customersGrid.DataSource = expanded.Customers;
        }
        else
        {
            _customersGrid.DataSource = new List<ExpandedCustomerRowDto>();
        }

        // 4. Payments
        if (expanded != null && expanded.Payments.Count > 0)
        {
            _paymentsGrid.DataSource = expanded.Payments;
        }
        else
        {
            _paymentsGrid.DataSource = report.CashSummary.Select(c => new ExpandedPaymentRowDto(
                c.PaymentMethodName,
                1,
                c.Total,
                report.TotalSales > 0 ? Math.Round(c.Total / report.TotalSales * 100m, 2) : 0m,
                "Order Settlement")).ToList();
        }

        // 5. Receivables Movement
        if (expanded != null && expanded.Receivables.Count > 0)
        {
            _receivablesGrid.DataSource = expanded.Receivables;
        }
        else
        {
            _receivablesGrid.DataSource = new List<ExpandedReceivableActivityRowDto>();
        }

        // 6. Order Types
        if (expanded != null && expanded.OrderTypes.Count > 0)
        {
            _orderTypesGrid.DataSource = expanded.OrderTypes;
        }
        else
        {
            _orderTypesGrid.DataSource = new List<ExpandedOrderTypeBreakdownDto>();
        }

        // 7. Item Types / Profitability
        if (expanded != null && expanded.ItemTypes.Count > 0)
        {
            _itemTypesGrid.DataSource = expanded.ItemTypes;
        }
        else
        {
            _itemTypesGrid.DataSource = new List<ExpandedItemClassificationBreakdownDto>();
        }

        // 8. Cash Summary (Cash Drawer Reconciliation by Shift)
        if (expanded?.ShiftDrawers != null && expanded.ShiftDrawers.Count > 0)
        {
            _cashSummaryGrid.DataSource = expanded.ShiftDrawers;
        }
        else
        {
            _cashSummaryGrid.DataSource = new List<ShiftDrawerCashSummaryDto>();
        }

        // 9. Inventory Movement
        var transactions = await _mediator.Send(new ListInventoryTransactionsByWarehouseQuery(warehouseId));
        _inventoryMovementGrid.DataSource = transactions
            .Where(t =>
            {
                var occurredDate = DateOnly.FromDateTime(t.OccurredAtUtc.UtcDateTime);
                return occurredDate >= fromDate && occurredDate <= toDate;
            })
            .OrderByDescending(t => t.OccurredAtUtc)
            .Select(t => new MovementRow(ResolveSku(t.ProductVariantId), ResolveName(t.ProductVariantId), t.TransactionType, t.Quantity, t.OccurredAtUtc))
            .ToList();

        // 10. Stock Remaining
        var stocks = await _mediator.Send(new ListWarehouseStocksByWarehouseQuery(warehouseId));
        _stockRemainingGrid.DataSource = stocks
            .Select(s => new StockRow(ResolveSku(s.ProductVariantId), ResolveName(s.ProductVariantId), s.QuantityOnHand, s.QuantityAvailable))
            .ToList();

        UpdateActionEnablement();
    }

    private void OrdersGridView_DoubleClick(object? sender, EventArgs e)
    {
        if (_ordersGridView.GetFocusedRow() is ExpandedOrderRowDto row)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Clovent Business Operating System");
            sb.AppendLine($"Order Details - {row.OrderNumber}");
            sb.AppendLine(new string('-', 40));
            sb.AppendLine($"Date:       {DateTimeDisplay.Format(row.CreatedAtUtc)}");
            sb.AppendLine($"Type:       {row.OrderType} ({row.OrderSource})");
            sb.AppendLine($"Customer:   {row.CustomerName}");
            sb.AppendLine($"Status:     {row.Status}");
            sb.AppendLine(new string('-', 40));
            if (row.Lines != null && row.Lines.Count > 0)
            {
                sb.AppendLine("Line Items:");
                foreach (var line in row.Lines)
                {
                    sb.AppendLine($"  {line.Quantity:0.##}x {line.ItemName} @ {CurrencyDisplay.Format(line.UnitPrice)} = {CurrencyDisplay.Format(line.LineTotal)}");
                }
                sb.AppendLine(new string('-', 40));
            }
            sb.AppendLine($"Subtotal:   {CurrencyDisplay.Format(row.Subtotal)}");
            if (row.Discount > 0) sb.AppendLine($"Discount:   -{CurrencyDisplay.Format(row.Discount)}");
            if (row.ServiceAndDeliveryFee > 0) sb.AppendLine($"Fee/Charge: +{CurrencyDisplay.Format(row.ServiceAndDeliveryFee)}");
            if (row.Tax > 0) sb.AppendLine($"Tax:        +{CurrencyDisplay.Format(row.Tax)}");
            sb.AppendLine($"Total:      {CurrencyDisplay.Format(row.Total)}");
            sb.AppendLine($"Paid:       {CurrencyDisplay.Format(row.PaidAmount)}");
            if (row.OnAccountAmount > 0) sb.AppendLine($"On Account: {CurrencyDisplay.Format(row.OnAccountAmount)}");
            sb.AppendLine($"Payment:    {row.PaymentSummary}");

            using var preview = new ReceiptPreviewForm(sb.ToString());
            preview.ShowDialog(this);
        }
    }

    private void PrintSummary()
    {
        if (string.IsNullOrEmpty(_summaryText))
        {
            XtraMessageBox.Show(this, "Generate a report first.", "Nothing to Print", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var preview = new ReceiptPreviewForm(_summaryText);
        preview.ShowDialog(this);
    }

    private static string BuildSummaryText(EndOfDayReportDto report, ExpandedSalesSummaryDto? expanded, DateOnly fromDate, DateOnly toDate)
    {
        var rangeText = fromDate == toDate ? $"{fromDate:yyyy-MM-dd}" : $"{fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Clovent Business Operating System");
        sb.AppendLine($"Sales Summary - {rangeText}");
        sb.AppendLine(new string('-', 40));
        sb.AppendLine($"Total Bills:         {expanded?.Kpis.TotalOrders ?? report.ReceiptCount}");
        sb.AppendLine($"Item Sales:          {CurrencyDisplay.Format(expanded?.Kpis.ItemSalesValue ?? report.TotalSales)}");
        if (expanded != null && expanded.Kpis.Discounts > 0)
        {
            sb.AppendLine($"Discount:            {CurrencyDisplay.Format(expanded.Kpis.Discounts)}");
        }
        if (expanded != null && expanded.Kpis.DeliveryFees > 0)
        {
            sb.AppendLine($"Delivery Fees:       {CurrencyDisplay.Format(expanded.Kpis.DeliveryFees)}");
        }
        if (expanded != null && expanded.Kpis.ServiceCharges > 0)
        {
            sb.AppendLine($"Service Charges:     {CurrencyDisplay.Format(expanded.Kpis.ServiceCharges)}");
        }
        if (expanded != null && expanded.Kpis.Tax > 0)
        {
            sb.AppendLine($"Tax:                 {CurrencyDisplay.Format(expanded.Kpis.Tax)}");
        }
        sb.AppendLine($"Total Bill Sales:    {CurrencyDisplay.Format(expanded?.Kpis.TotalBillSalesValue ?? report.TotalSales)}");
        sb.AppendLine($"Cash:                {CurrencyDisplay.Format(expanded?.Kpis.CashCollected ?? report.CashCollected)}");
        sb.AppendLine($"Card:                {CurrencyDisplay.Format(expanded?.Kpis.CardCollected ?? report.CardCollected)}");
        if (expanded != null && expanded.Kpis.OnAccountCreated > 0)
        {
            sb.AppendLine($"On Account:          {CurrencyDisplay.Format(expanded.Kpis.OnAccountCreated)}");
        }
        sb.AppendLine($"Voided Orders:       {expanded?.Kpis.VoidedOrdersCount ?? report.VoidedOrderCount}");
        sb.AppendLine($"Average Sale / Bill: {CurrencyDisplay.Format(expanded?.Kpis.AverageOrderValue ?? report.AverageSale)}");
        sb.AppendLine(new string('-', 40));
        if (expanded?.ShiftDrawers != null && expanded.ShiftDrawers.Count > 0)
        {
            sb.AppendLine("Cash Drawer Reconciliation (by Shift):");
            foreach (var d in expanded.ShiftDrawers)
            {
                var countedStr = d.CountedCash.HasValue ? CurrencyDisplay.Format(d.CountedCash.Value) : "N/A";
                var varianceStr = d.Variance.HasValue ? CurrencyDisplay.Format(d.Variance.Value) : "N/A";
                sb.AppendLine($"  Shift #{d.ShiftNumber} ({d.CashierName}): Expected {CurrencyDisplay.Format(d.ExpectedCash)}, Counted {countedStr}, Variance {varianceStr} [{d.Status}]");
            }
        }
        else
        {
            sb.AppendLine("Cash Summary:");
            foreach (var method in report.CashSummary)
            {
                sb.AppendLine($"  {method.PaymentMethodName}: {CurrencyDisplay.Format(method.Total)}");
            }
        }

        if (expanded != null && expanded.OrderTypes.Count > 0)
        {
            sb.AppendLine(new string('-', 40));
            sb.AppendLine("Sales by Order Type:");
            foreach (var ot in expanded.OrderTypes)
            {
                sb.AppendLine($"  {ot.OrderType}: {ot.OrdersCount} orders, {CurrencyDisplay.Format(ot.TotalSales)} ({ot.PercentOfTotal:F1}%)");
            }
        }

        if (expanded != null && expanded.ItemTypes.Count > 0)
        {
            sb.AppendLine(new string('-', 40));
            sb.AppendLine("Sales by Item Classification:");
            foreach (var it in expanded.ItemTypes)
            {
                var costStr = it.TotalCost.HasValue ? CurrencyDisplay.Format(it.TotalCost.Value) : "N/A";
                var profitStr = it.GrossProfit.HasValue ? CurrencyDisplay.Format(it.GrossProfit.Value) : "N/A";
                var marginStr = it.MarginPercent.HasValue ? $"{it.MarginPercent.Value:F1}%" : "N/A";
                sb.AppendLine($"  {it.ItemType}: {CurrencyDisplay.Format(it.TotalSales)} (Cost: {costStr}, Profit: {profitStr}, Margin: {marginStr})");
            }
        }

        if (expanded != null && expanded.Receivables.Count > 0)
        {
            var totalOnAccount = expanded.Receivables.Sum(r => r.NewOnAccountSales);
            var totalPayments = expanded.Receivables.Sum(r => r.CustomerPayments);
            var netChange = totalOnAccount - totalPayments;
            if (totalOnAccount > 0 || totalPayments > 0)
            {
                sb.AppendLine(new string('-', 40));
                sb.AppendLine("Customer Receivables Activity:");
                sb.AppendLine($"  + On Account:    {CurrencyDisplay.Format(totalOnAccount)}");
                sb.AppendLine($"  - Collections:   {CurrencyDisplay.Format(totalPayments)}");
                sb.AppendLine($"  Net Change:      {CurrencyDisplay.Format(netChange)}");
            }
        }

        return sb.ToString();
    }

    private string ResolveSku(Guid variantId) =>
        _variantsById.TryGetValue(variantId, out var info) && !string.IsNullOrEmpty(info.Sku)
            ? info.Sku
            : "-";

    private string ResolveName(Guid variantId)
    {
        if (!_variantsById.TryGetValue(variantId, out var info))
        {
            return "-";
        }

        return string.IsNullOrEmpty(info.ProductName) || info.ProductName == info.VariantName
            ? info.VariantName
            : $"{info.ProductName} - {info.VariantName}";
    }

    private sealed record ItemSoldRow(string Sku, string Name, decimal Quantity, decimal Total);

    private sealed record BillRow(string OrderNumber, DateTimeOffset CompletedAtUtc, decimal Total, string PaymentMethodSummary);

    private sealed record MovementRow(string Sku, string Name, string TransactionType, decimal Quantity, DateTimeOffset OccurredAtUtc);

    private sealed record StockRow(string Sku, string Name, decimal QuantityOnHand, decimal QuantityAvailable);
}
