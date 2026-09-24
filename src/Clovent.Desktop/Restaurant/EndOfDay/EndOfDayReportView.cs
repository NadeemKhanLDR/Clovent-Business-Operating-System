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
/// Sales Summary - the Restaurant owner's name for what the domain/Application
/// layer still calls the Day-End / Z-report (<c>GetEndOfDayReportQuery</c>,
/// unchanged): Total Bills, Total Sales, Cash, Card, Top Selling Items,
/// Bills, plus Inventory Movement and Stock Remaining composed from
/// <c>Clovent.Inventory.Application</c>'s existing queries.
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
            var range = ReportPeriodCalculator.CalculateRange(period, DateOnly.FromDateTime(DateTime.Today));
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
        1 => _itemsSoldGrid,
        2 => _cashSummaryGrid,
        3 => _billsGrid,
        4 => _inventoryMovementGrid,
        5 => _stockRemainingGrid,
        _ => null
    };

    private string GetCurrentTabExportName() => _tabControl.SelectedTabPageIndex switch
    {
        1 => "TopSellingItems",
        2 => "CashSummary",
        3 => "Bills",
        4 => "InventoryMovement",
        5 => "StockRemaining",
        _ => "SalesSummary"
    };

    private void PreviewButton_Click(object? sender, EventArgs e)
    {
        if (_tabControl.SelectedTabPageIndex == 0)
        {
            PrintSummary();
            return;
        }

        GetCurrentTabGrid()?.ShowPrintPreview();
    }

    private void PrintButton_Click(object? sender, EventArgs e)
    {
        if (_tabControl.SelectedTabPageIndex == 0)
        {
            PrintSummary();
            return;
        }

        GetCurrentTabGrid()?.ShowRibbonPrintPreview();
    }

    private void ExportPdfButton_Click(object? sender, EventArgs e)
    {
        var grid = GetCurrentTabGrid();
        if (grid is null) return;
        var name = GetCurrentTabExportName();
        ExportGrid(grid, "PDF files (*.pdf)|*.pdf", $"{name}.pdf", (g, path) => g.ExportToPdf(path));
    }

    private void ExportExcelButton_Click(object? sender, EventArgs e)
    {
        var grid = GetCurrentTabGrid();
        if (grid is null) return;
        var name = GetCurrentTabExportName();
        ExportGrid(grid, "Excel files (*.xlsx)|*.xlsx", $"{name}.xlsx", (g, path) => g.ExportToXlsx(path));
    }

    private async void EndOfDayReportView_Load(object? sender, EventArgs e)
    {
        _periodCombo.MinimumSize = new Size(Clovent.Desktop.Forms.Base.DesktopDpi.Scale(130, this), Clovent.Desktop.Forms.Base.DesktopDpi.Scale(32, this));
        _fromDateEdit.MinimumSize = new Size(Clovent.Desktop.Forms.Base.DesktopDpi.Scale(135, this), Clovent.Desktop.Forms.Base.DesktopDpi.Scale(32, this));
        _toDateEdit.MinimumSize = new Size(Clovent.Desktop.Forms.Base.DesktopDpi.Scale(135, this), Clovent.Desktop.Forms.Base.DesktopDpi.Scale(32, this));
        _generateButton.MinimumSize = new Size(Clovent.Desktop.Forms.Base.DesktopDpi.Scale(110, this), Clovent.Desktop.Forms.Base.DesktopDpi.Scale(34, this));
        _previewButton.MinimumSize = new Size(Clovent.Desktop.Forms.Base.DesktopDpi.Scale(80, this), Clovent.Desktop.Forms.Base.DesktopDpi.Scale(34, this));
        _printButton.MinimumSize = new Size(Clovent.Desktop.Forms.Base.DesktopDpi.Scale(70, this), Clovent.Desktop.Forms.Base.DesktopDpi.Scale(34, this));
        _exportPdfButton.MinimumSize = new Size(Clovent.Desktop.Forms.Base.DesktopDpi.Scale(95, this), Clovent.Desktop.Forms.Base.DesktopDpi.Scale(34, this));
        _exportExcelButton.MinimumSize = new Size(Clovent.Desktop.Forms.Base.DesktopDpi.Scale(100, this), Clovent.Desktop.Forms.Base.DesktopDpi.Scale(34, this));
        _printSummaryButton.MinimumSize = new Size(Clovent.Desktop.Forms.Base.DesktopDpi.Scale(130, this), Clovent.Desktop.Forms.Base.DesktopDpi.Scale(34, this));

        UpdateActionEnablement();
        AppearanceManager.Apply(this, "Restaurant", nameof(EndOfDayReportView));
        await LoadAndShowTodayAsync();
    }

    /// <summary>
    /// Loads locations and immediately shows Today's figures - a restaurant
    /// owner opening this screen for the first time should see today's
    /// sales right away, not a blank "0.00" dashboard waiting for a click
    /// they don't know to make.
    /// </summary>
    private async Task LoadAndShowTodayAsync()
    {
        await LoadWarehousesAsync();

        // Only auto-generate once there is actually a location to report
        // on - silently doing nothing here (rather than GenerateAsync's own
        // "Select a location first" warning) avoids popping a dialog the
        // instant this screen opens, before the owner has done anything.
        if (_warehousePicker.SelectedId is not null)
        {
            await SetDateRangeAndGenerateAsync(DateTime.UtcNow.Date, DateTime.UtcNow.Date);
        }
    }

    /// <summary>Sets both date edits and regenerates - backs the Today/Yesterday one-click quick filters.</summary>
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

    /// <summary>The half-dozen sequential reads behind one Generate (report, every variant, every transaction, every stock line) are the slowest single action in this screen - worth a wait cursor, unlike the quick per-tab Preview/Print/Export actions.</summary>
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

        var report = await _mediator.Send(new GetEndOfDayReportQuery(warehouseId, fromDate, toDate));

        _totalBillsValueLabel.Text = report.ReceiptCount.ToString();
        _totalSalesValueLabel.Text = CurrencyDisplay.Format(report.TotalSales);
        _cashValueLabel.Text = CurrencyDisplay.Format(report.CashCollected);
        _cardValueLabel.Text = CurrencyDisplay.Format(report.CardCollected);
        _voidedCountLabel.Text = $"{report.VoidedOrderCount}";
        _averageSaleLabel.Text = CurrencyDisplay.Format(report.AverageSale);
        _summaryText = BuildSummaryText(report, fromDate, toDate);

        // Presentation-only: an empty period shows the professional empty
        // state in the Summary body instead of a blank white area.
        _summaryEmptyStateLabel.Visible = report.ReceiptCount == 0;

        _itemsSoldGrid.DataSource = report.ItemsSold
            .Select(i => new ItemSoldRow(ResolveSku(i.ProductVariantId), ResolveName(i.ProductVariantId), i.Quantity, i.Total))
            .ToList();

        _cashSummaryGrid.DataSource = report.CashSummary.ToList();

        _billsGrid.DataSource = report.Bills
            .Select(b => new BillRow(b.OrderNumber, b.CompletedAtUtc, b.Total, b.PaymentMethodSummary))
            .ToList();

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

        var stocks = await _mediator.Send(new ListWarehouseStocksByWarehouseQuery(warehouseId));
        _stockRemainingGrid.DataSource = stocks
            .Select(s => new StockRow(ResolveSku(s.ProductVariantId), ResolveName(s.ProductVariantId), s.QuantityOnHand, s.QuantityAvailable))
            .ToList();

        UpdateActionEnablement();
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

    private static string BuildSummaryText(EndOfDayReportDto report, DateOnly fromDate, DateOnly toDate)
    {
        var rangeText = fromDate == toDate ? $"{fromDate:yyyy-MM-dd}" : $"{fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Clovent Business Operating System");
        sb.AppendLine($"Sales Summary - {rangeText}");
        sb.AppendLine(new string('-', 40));
        sb.AppendLine($"Total Bills:     {report.ReceiptCount}");
        sb.AppendLine($"Total Sales:     {CurrencyDisplay.Format(report.TotalSales)}");
        sb.AppendLine($"Cash:            {CurrencyDisplay.Format(report.CashCollected)}");
        sb.AppendLine($"Card:            {CurrencyDisplay.Format(report.CardCollected)}");
        sb.AppendLine($"Voided Orders:   {report.VoidedOrderCount}");
        sb.AppendLine($"Average Sale:    {CurrencyDisplay.Format(report.AverageSale)}");
        sb.AppendLine(new string('-', 40));
        sb.AppendLine("Cash Summary:");
        foreach (var method in report.CashSummary)
        {
            sb.AppendLine($"  {method.PaymentMethodName}: {CurrencyDisplay.Format(method.Total)}");
        }

        return sb.ToString();
    }

    private string ResolveSku(Guid variantId) => _variantsById.TryGetValue(variantId, out var v) ? v.Sku : "(unknown)";

    private string ResolveName(Guid variantId)
    {
        if (!_variantsById.TryGetValue(variantId, out var info))
        {
            return "(unknown)";
        }

        var (_, variantName, productName) = info;
        if (string.IsNullOrWhiteSpace(productName))
        {
            return string.IsNullOrWhiteSpace(variantName) ? "(unknown)" : variantName;
        }

        if (string.IsNullOrWhiteSpace(variantName) ||
            variantName.Equals(productName, StringComparison.OrdinalIgnoreCase) ||
            variantName == "-")
        {
            return productName;
        }

        if (variantName.StartsWith(productName, StringComparison.OrdinalIgnoreCase))
        {
            return variantName;
        }

        return $"{productName} - {variantName}";
    }

    private sealed record ItemSoldRow(string Sku, string Name, decimal Quantity, decimal Total);

    private sealed record MovementRow(string Sku, string Name, string TransactionType, decimal Quantity, DateTimeOffset OccurredAtUtc);

    private sealed record StockRow(string Sku, string Name, decimal QuantityOnHand, decimal QuantityAvailable);

    private sealed record BillRow(string OrderNumber, DateTimeOffset CompletedAtUtc, decimal Total, string PaymentMethodSummary);
}
