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
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Restaurant.EndOfDay;

/// <summary>
/// Sales Summary - the Restaurant owner's name for what the domain/Application
/// layer still calls the Day-End / Z-report (<c>GetEndOfDayReportQuery</c>,
/// unchanged): Total Bills, Total Sales, Cash, Card, Top Selling Items,
/// Bills, plus Inventory Movement and Stock Remaining composed from
/// <c>Clovent.Inventory.Application</c>'s existing queries. Only this
/// presentation layer's captions changed for the Restaurant UX refinement -
/// every figure, query, and command underneath is exactly what
/// <c>RestaurantPOSArchitecture.md</c> already documents. One tab per
/// section so each grid keeps its own native DevExpress Preview/Print/Export
/// PDF/Export Excel actions - see this screen's own commit/architecture note
/// for why a single combined print document was not attempted. Feature-gated
/// per <c>endofday.view</c> (the feature code itself was left unchanged -
/// renaming it would have no user-visible effect and would only add churn to
/// every seeded permission referencing it).
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

    private Dictionary<Guid, (string Sku, string Name)> _variantsById = [];
    private string _summaryText = string.Empty;

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public EndOfDayReportView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = new SerializedMediator(_scope.ServiceProvider.GetRequiredService<IMediator>(), _gate);
        _featurePolicy = new SerializedFeatureAuthorizationPolicy(_scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>(), _gate);
        _currentSession = currentSession;

        AppearanceManager.Changed += AppearanceManager_Changed;

        InitializeComponent();
    }

    private void AppearanceManager_Changed(object? sender, EventArgs e) => AppearanceManager.Apply(this, "Restaurant", nameof(EndOfDayReportView));

    private async void TodayButton_Click(object? sender, EventArgs e) => await SetDateRangeAndGenerateAsync(DateTime.UtcNow.Date, DateTime.UtcNow.Date);

    private async void YesterdayButton_Click(object? sender, EventArgs e) => await SetDateRangeAndGenerateAsync(DateTime.UtcNow.Date.AddDays(-1), DateTime.UtcNow.Date.AddDays(-1));

    private async void GenerateButton_Click(object? sender, EventArgs e) => await GenerateAsync();

    private void PrintSummaryButton_Click(object? sender, EventArgs e) => PrintSummary();

    private async void EndOfDayReportView_Load(object? sender, EventArgs e)
    {
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
        _fromDateEdit.EditValue = from;
        _toDateEdit.EditValue = to;
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
        _warehousePicker.LoadItems([.. warehouses.Select(w => (w.WarehouseId, w.Name))]);
        _warehousePicker.Visible = warehouses.Count > 1;
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
        var currencies = await _mediator.Send(new ListCurrenciesQuery());
        if (currencies.FirstOrDefault() is { } currency)
        {
            CurrencyDisplay.Configure(currency.Symbol, currency.DecimalPlaces);
        }

        var variants = await _mediator.Send(new ListProductVariantsQuery());
        _variantsById = variants.ToDictionary(v => v.ProductVariantId, v => (v.Sku, v.Name));

        var report = await _mediator.Send(new GetEndOfDayReportQuery(warehouseId, fromDate, toDate));

        _totalBillsValueLabel.Text = report.ReceiptCount.ToString();
        _totalSalesValueLabel.Text = CurrencyDisplay.Format(report.TotalSales);
        _cashValueLabel.Text = CurrencyDisplay.Format(report.CashCollected);
        _cardValueLabel.Text = CurrencyDisplay.Format(report.CardCollected);
        _voidedCountLabel.Text = $"Voided Orders: {report.VoidedOrderCount}";
        _averageSaleLabel.Text = $"Average Sale: {CurrencyDisplay.Format(report.AverageSale)}";
        _summaryText = BuildSummaryText(report, fromDate, toDate);

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

    private string ResolveName(Guid variantId) => _variantsById.TryGetValue(variantId, out var v) ? v.Name : "(unknown)";

    private sealed record ItemSoldRow(string Sku, string Name, decimal Quantity, decimal Total);

    private sealed record MovementRow(string Sku, string Name, string TransactionType, decimal Quantity, DateTimeOffset OccurredAtUtc);

    private sealed record StockRow(string Sku, string Name, decimal QuantityOnHand, decimal QuantityAvailable);

    private sealed record BillRow(string OrderNumber, DateTimeOffset CompletedAtUtc, decimal Total, string PaymentMethodSummary);
}
