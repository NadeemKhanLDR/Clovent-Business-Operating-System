using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Desktop.MasterData;
using Clovent.Desktop.Restaurant.Orders;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Inventory.Application.Transactions.Dtos;
using Clovent.Inventory.Application.Transactions.Queries;
using Clovent.Inventory.Application.WarehouseStocks.Dtos;
using Clovent.Inventory.Application.WarehouseStocks.Queries;
using Clovent.MasterData.Application.Warehouses.Queries;
using Clovent.Restaurant.Application.EndOfDay.Dtos;
using Clovent.Restaurant.Application.EndOfDay.Queries;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraPrinting;
using DevExpress.XtraTab;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Restaurant.EndOfDay;

/// <summary>
/// End-of-Day (Day-End / Z-report) screen: Today's Sales, Cash Collected,
/// Items Sold (doubling as Top Selling Items - see
/// <c>GetEndOfDayReportQuery</c>), Cash Summary, Receipt Count, Transaction
/// Summary (voided count), Average Sale, plus Inventory Movement and Stock
/// Remaining composed from <c>Clovent.Inventory.Application</c>'s existing
/// queries. One tab per section so each grid keeps its own native DevExpress
/// Preview/Print/Export PDF/Export Excel actions - see this screen's own
/// commit/architecture note for why a single combined print document was not
/// attempted. Feature-gated per <c>endofday.view</c>.
/// </summary>
public sealed class EndOfDayReportView : XtraUserControl
{
    private const string FeatureCode = "endofday";

    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;

    private readonly EntityPicker _warehousePicker = new("Warehouse:");
    private readonly DateEdit _dateEdit = new() { EditValue = DateTime.UtcNow.Date };
    private readonly SimpleButton _generateButton = new() { Text = "Generate" };

    private readonly LabelControl _totalSalesLabel = new();
    private readonly LabelControl _cashCollectedLabel = new();
    private readonly LabelControl _receiptCountLabel = new();
    private readonly LabelControl _voidedCountLabel = new();
    private readonly LabelControl _averageSaleLabel = new();
    private readonly SimpleButton _printSummaryButton = new() { Text = "Print Summary" };

    private readonly GridControl _itemsSoldGrid = new() { Dock = DockStyle.Fill };
    private readonly GridView _itemsSoldGridView = new();
    private readonly GridControl _cashSummaryGrid = new() { Dock = DockStyle.Fill };
    private readonly GridView _cashSummaryGridView = new();
    private readonly GridControl _inventoryMovementGrid = new() { Dock = DockStyle.Fill };
    private readonly GridView _inventoryMovementGridView = new();
    private readonly GridControl _stockRemainingGrid = new() { Dock = DockStyle.Fill };
    private readonly GridView _stockRemainingGridView = new();

    private Dictionary<Guid, (string Sku, string Name)> _variantsById = [];
    private string _summaryText = string.Empty;

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public EndOfDayReportView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _featurePolicy = _scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>();
        _currentSession = currentSession;

        Dock = DockStyle.Fill;

        BuildGrid(_itemsSoldGrid, _itemsSoldGridView,
        [
            (nameof(ItemSoldRow.Sku), "SKU", 100),
            (nameof(ItemSoldRow.Name), "Product", 200),
            (nameof(ItemSoldRow.Quantity), "Quantity", 90),
            (nameof(ItemSoldRow.Total), "Total", 100),
        ]);

        BuildGrid(_cashSummaryGrid, _cashSummaryGridView,
        [
            (nameof(EndOfDayPaymentMethodTotalDto.PaymentMethodName), "Payment Method", 180),
            (nameof(EndOfDayPaymentMethodTotalDto.Total), "Total", 120),
        ]);

        BuildGrid(_inventoryMovementGrid, _inventoryMovementGridView,
        [
            (nameof(MovementRow.Sku), "SKU", 100),
            (nameof(MovementRow.Name), "Product", 160),
            (nameof(MovementRow.TransactionType), "Type", 100),
            (nameof(MovementRow.Quantity), "Quantity", 90),
            (nameof(MovementRow.OccurredAtUtc), "Occurred (UTC)", 160),
        ]);

        BuildGrid(_stockRemainingGrid, _stockRemainingGridView,
        [
            (nameof(StockRow.Sku), "SKU", 100),
            (nameof(StockRow.Name), "Product", 160),
            (nameof(StockRow.QuantityOnHand), "On Hand", 90),
            (nameof(StockRow.QuantityAvailable), "Available", 90),
        ]);

        var tabControl = new XtraTabControl { Dock = DockStyle.Fill };
        tabControl.TabPages.Add(BuildSummaryPage());
        tabControl.TabPages.Add(BuildGridPage("Items Sold / Top Selling", _itemsSoldGrid, "itemssold"));
        tabControl.TabPages.Add(BuildGridPage("Cash Summary", _cashSummaryGrid, "cashsummary"));
        tabControl.TabPages.Add(BuildGridPage("Inventory Movement", _inventoryMovementGrid, "inventorymovement"));
        tabControl.TabPages.Add(BuildGridPage("Stock Remaining", _stockRemainingGrid, "stockremaining"));

        var topPanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(4) };
        _dateEdit.Width = 120;
        topPanel.Controls.Add(_warehousePicker);
        topPanel.Controls.Add(new LabelControl { Text = "Date:", Padding = new Padding(8, 6, 4, 0) });
        topPanel.Controls.Add(_dateEdit);
        topPanel.Controls.Add(_generateButton);

        Controls.Add(tabControl);
        Controls.Add(topPanel);

        _generateButton.Click += async (_, _) => await GenerateAsync();
        _printSummaryButton.Click += (_, _) => PrintSummary();

        Load += async (_, _) => await LoadWarehousesAsync();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _scope.Dispose();
        }

        base.Dispose(disposing);
    }

    private static void BuildGrid(GridControl grid, GridView view, (string FieldName, string Caption, int Width)[] columns)
    {
        grid.MainView = view;
        grid.ViewCollection.Add(view);
        view.OptionsBehavior.Editable = false;
        view.OptionsSelection.MultiSelect = false;
        view.OptionsView.ShowGroupPanel = false;

        foreach (var (fieldName, caption, width) in columns)
        {
            view.Columns.AddVisible(fieldName, caption).Width = width;
        }
    }

    private XtraTabPage BuildSummaryPage()
    {
        var page = new XtraTabPage { Text = "Summary" };
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(12) };

        foreach (var label in new[] { _totalSalesLabel, _cashCollectedLabel, _receiptCountLabel, _voidedCountLabel, _averageSaleLabel })
        {
            label.Font = new Font(Font.FontFamily, 11f);
            panel.Controls.Add(label);
        }

        panel.Controls.Add(new SeparatorControl { Width = 260 });
        panel.Controls.Add(_printSummaryButton);

        page.Controls.Add(panel);
        return page;
    }

    private XtraTabPage BuildGridPage(string title, GridControl grid, string featureOperation)
    {
        var page = new XtraTabPage { Text = title };

        var previewButton = new SimpleButton { Text = "Preview" };
        var printButton = new SimpleButton { Text = "Print" };
        var exportPdfButton = new SimpleButton { Text = "Export PDF" };
        var exportExcelButton = new SimpleButton { Text = "Export Excel" };

        previewButton.Click += (_, _) => grid.ShowPrintPreview();
        printButton.Click += (_, _) => grid.ShowRibbonPrintPreview();
        exportPdfButton.Click += (_, _) => ExportGrid(grid, "PDF files (*.pdf)|*.pdf", $"{featureOperation}.pdf", (g, path) => g.ExportToPdf(path));
        exportExcelButton.Click += (_, _) => ExportGrid(grid, "Excel files (*.xlsx)|*.xlsx", $"{featureOperation}.xlsx", (g, path) => g.ExportToXlsx(path));

        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 32, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        toolbar.Controls.Add(previewButton);
        toolbar.Controls.Add(printButton);
        toolbar.Controls.Add(exportPdfButton);
        toolbar.Controls.Add(exportExcelButton);

        page.Controls.Add(grid);
        page.Controls.Add(toolbar);
        return page;
    }

    private void ExportGrid(GridControl grid, string filter, string fileName, Action<GridControl, string> export)
    {
        using var dialog = new SaveFileDialog { Filter = filter, FileName = fileName };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            export(grid, dialog.FileName);
        }
    }

    private async Task LoadWarehousesAsync()
    {
        var warehouses = await _mediator.Send(new ListAllWarehousesQuery());
        _warehousePicker.LoadItems([.. warehouses.Select(w => (w.WarehouseId, w.Name))]);
    }

    private async Task GenerateAsync()
    {
        if (_warehousePicker.SelectedId is not { } warehouseId)
        {
            XtraMessageBox.Show(this, "Select a warehouse first.", "No Warehouse Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var date = DateOnly.FromDateTime((DateTime)_dateEdit.EditValue);

        var variants = await _mediator.Send(new ListProductVariantsQuery());
        _variantsById = variants.ToDictionary(v => v.ProductVariantId, v => (v.Sku, v.Name));

        var report = await _mediator.Send(new GetEndOfDayReportQuery(warehouseId, date));

        _totalSalesLabel.Text = $"Today's Sales: {report.TotalSales:N2}";
        _cashCollectedLabel.Text = $"Cash Collected: {report.CashCollected:N2}";
        _receiptCountLabel.Text = $"Receipt Count: {report.ReceiptCount}";
        _voidedCountLabel.Text = $"Voided Orders: {report.VoidedOrderCount}";
        _averageSaleLabel.Text = $"Average Sale: {report.AverageSale:N2}";
        _summaryText = BuildSummaryText(report, date);

        _itemsSoldGrid.DataSource = report.ItemsSold
            .Select(i => new ItemSoldRow(ResolveSku(i.ProductVariantId), ResolveName(i.ProductVariantId), i.Quantity, i.Total))
            .ToList();

        _cashSummaryGrid.DataSource = report.CashSummary.ToList();

        var transactions = await _mediator.Send(new ListInventoryTransactionsByWarehouseQuery(warehouseId));
        _inventoryMovementGrid.DataSource = transactions
            .Where(t => DateOnly.FromDateTime(t.OccurredAtUtc.UtcDateTime) == date)
            .OrderByDescending(t => t.OccurredAtUtc)
            .Select(t => new MovementRow(ResolveSku(t.ProductVariantId), ResolveName(t.ProductVariantId), t.TransactionType, t.Quantity, t.OccurredAtUtc))
            .ToList();

        var stocks = await _mediator.Send(new ListWarehouseStocksByWarehouseQuery(warehouseId));
        _stockRemainingGrid.DataSource = stocks
            .Select(s => new StockRow(ResolveSku(s.ProductVariantId), ResolveName(s.ProductVariantId), s.QuantityOnHand, s.QuantityAvailable))
            .ToList();
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

    private static string BuildSummaryText(EndOfDayReportDto report, DateOnly date)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Clovent Business Operating System");
        sb.AppendLine($"End-of-Day Report - {date:yyyy-MM-dd}");
        sb.AppendLine(new string('-', 40));
        sb.AppendLine($"Today's Sales:   {report.TotalSales:N2}");
        sb.AppendLine($"Cash Collected:  {report.CashCollected:N2}");
        sb.AppendLine($"Receipt Count:   {report.ReceiptCount}");
        sb.AppendLine($"Voided Orders:   {report.VoidedOrderCount}");
        sb.AppendLine($"Average Sale:    {report.AverageSale:N2}");
        sb.AppendLine(new string('-', 40));
        sb.AppendLine("Cash Summary:");
        foreach (var method in report.CashSummary)
        {
            sb.AppendLine($"  {method.PaymentMethodName}: {method.Total:N2}");
        }

        return sb.ToString();
    }

    private string ResolveSku(Guid variantId) => _variantsById.TryGetValue(variantId, out var v) ? v.Sku : "(unknown)";

    private string ResolveName(Guid variantId) => _variantsById.TryGetValue(variantId, out var v) ? v.Name : "(unknown)";

    private sealed record ItemSoldRow(string Sku, string Name, decimal Quantity, decimal Total);

    private sealed record MovementRow(string Sku, string Name, string TransactionType, decimal Quantity, DateTimeOffset OccurredAtUtc);

    private sealed record StockRow(string Sku, string Name, decimal QuantityOnHand, decimal QuantityAvailable);
}
