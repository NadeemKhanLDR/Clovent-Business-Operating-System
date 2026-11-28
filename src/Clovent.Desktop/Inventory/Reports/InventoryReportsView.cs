using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Sessions;
using Clovent.Inventory.Application.Reports.Dtos;
using Clovent.Inventory.Application.Reports.Queries;
using Clovent.MasterData.Application.Warehouses.Queries;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Inventory.Reports;

/// <summary>
/// Comprehensive back-office reporting view for restaurant inventory management.
/// Integrates Recipe Food Costing, Ingredient Costs, Asset Valuation, Stock Variance, and Kitchen Wastage.
/// </summary>
[DesignerCategory("Code")]
public sealed partial class InventoryReportsView : XtraUserControl
{
    private readonly IServiceScope _scope;
    private readonly ScreenOperationGate _gate = new();
    private readonly IMediator _mediator;
    private readonly ICurrentSession _currentSession;

    private readonly Dictionary<string, Guid?> _warehousesByDisplay = [];
    private bool _isUpdatingPeriod;

    /// <summary>Design-time constructor.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public InventoryReportsView()
    {
        _scope = null!;
        _mediator = null!;
        _currentSession = null!;
        InitializeComponent();
    }

    /// <summary>Initializes a new instance of <see cref="InventoryReportsView"/>.</summary>
    public InventoryReportsView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        InitializeComponent();

        if (DesignModeHelper.IsInDesignMode)
        {
            _scope = null!;
            _mediator = null!;
            _currentSession = null!;
            return;
        }

        _scope = scopeFactory.CreateScope();
        _mediator = new SerializedMediator(_scope.ServiceProvider.GetRequiredService<IMediator>(), _gate);
        _currentSession = currentSession;

        WireEvents();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _scope?.Dispose();
            _gate?.Dispose();
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void WireEvents()
    {
        Load += async (s, e) =>
        {
            ConfigurePeriods();
            await LoadWarehousesAsync();
            await RefreshActiveReportAsync();
        };

        _periodCombo.SelectedIndexChanged += (s, e) => OnPeriodChanged();
        _fromDateEdit.EditValueChanged += (s, e) => OnCustomDateSelected();
        _toDateEdit.EditValueChanged += (s, e) => OnCustomDateSelected();
        _warehouseCombo.SelectedIndexChanged += async (s, e) => await RefreshActiveReportAsync();
        _refreshButton.Click += async (s, e) => await RefreshActiveReportAsync();

        _tabControl.SelectedPageChanged += async (s, e) =>
        {
            UpdateFilterVisibilityForActiveTab();
            await RefreshActiveReportAsync();
        };

        _exportExcelBtn.Click += (s, e) => ExportActiveReportToExcel();
        _exportPdfBtn.Click += (s, e) => ExportActiveReportToPdf();
        _printPreviewBtn.Click += (s, e) => ShowActiveReportPrintPreview();
    }

    private void ConfigurePeriods()
    {
        _periodCombo.Properties.Items.Clear();
        _periodCombo.Properties.Items.AddRange(new object[] { "Today", "Yesterday", "This Week", "This Month", "Last 30 Days", "Custom" });
        _periodCombo.SelectedItem = "This Month";
    }

    private void OnPeriodChanged()
    {
        if (_isUpdatingPeriod) return;
        _isUpdatingPeriod = true;
        try
        {
            var now = DateTime.Today;
            switch (_periodCombo.SelectedItem?.ToString())
            {
                case "Today":
                    _fromDateEdit.DateTime = now;
                    _toDateEdit.DateTime = now.AddDays(1).AddSeconds(-1);
                    break;
                case "Yesterday":
                    _fromDateEdit.DateTime = now.AddDays(-1);
                    _toDateEdit.DateTime = now.AddSeconds(-1);
                    break;
                case "This Week":
                    int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
                    _fromDateEdit.DateTime = now.AddDays(-1 * diff);
                    _toDateEdit.DateTime = now.AddDays(1).AddSeconds(-1);
                    break;
                case "This Month":
                    _fromDateEdit.DateTime = new DateTime(now.Year, now.Month, 1);
                    _toDateEdit.DateTime = now.AddDays(1).AddSeconds(-1);
                    break;
                case "Last 30 Days":
                    _fromDateEdit.DateTime = now.AddDays(-30);
                    _toDateEdit.DateTime = now.AddDays(1).AddSeconds(-1);
                    break;
            }
        }
        finally
        {
            _isUpdatingPeriod = false;
        }
    }

    private void OnCustomDateSelected()
    {
        if (_isUpdatingPeriod) return;
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

    private async Task LoadWarehousesAsync()
    {
        try
        {
            var warehouses = await _mediator.Send(new ListAllWarehousesQuery());
            _warehouseCombo.Properties.Items.Clear();
            _warehousesByDisplay.Clear();

            _warehousesByDisplay["All Warehouses"] = null;
            _warehouseCombo.Properties.Items.Add("All Warehouses");

            foreach (var w in warehouses)
            {
                var display = $"{w.Code} - {w.Name}";
                _warehousesByDisplay[display] = w.WarehouseId;
                _warehouseCombo.Properties.Items.Add(display);
            }

            _warehouseCombo.SelectedIndex = 0;
        }
        catch
        {
            // Warehouse lookup unavailable
        }
    }

    private void UpdateFilterVisibilityForActiveTab()
    {
        int pageIndex = _tabControl.SelectedTabPageIndex;

        // Date range is relevant for Stock Variance (3) and Wastage (4)
        bool usesDateRange = pageIndex is 3 or 4;
        _periodCombo.Enabled = usesDateRange;
        _fromDateEdit.Enabled = usesDateRange;
        _toDateEdit.Enabled = usesDateRange;

        // Food cost alert is relevant for Recipe Cost (0)
        _alertThresholdEdit.Enabled = pageIndex == 0;
    }

    private Guid? GetSelectedWarehouseId()
    {
        if (_warehouseCombo.SelectedItem is string display && _warehousesByDisplay.TryGetValue(display, out var wid))
        {
            return wid;
        }
        return null;
    }

    private async Task RefreshActiveReportAsync()
    {
        Cursor = Cursors.WaitCursor;
        _refreshButton.Enabled = false;

        try
        {
            int pageIndex = _tabControl.SelectedTabPageIndex;
            var warehouseId = GetSelectedWarehouseId();
            var fromUtc = new DateTimeOffset(_fromDateEdit.DateTime.ToUniversalTime());
            var toUtc = new DateTimeOffset(_toDateEdit.DateTime.ToUniversalTime());

            switch (pageIndex)
            {
                case 0: // Recipe Food Cost
                    decimal threshold = _alertThresholdEdit.Value > 0 ? _alertThresholdEdit.Value : 35m;
                    var recipeReport = await _mediator.Send(new GetRecipeCostReportQuery(threshold));
                    _recipeGrid.DataSource = recipeReport.Items;
                    _kpi1.Text = $"Total Active Recipes: {recipeReport.TotalRecipes}";
                    _kpi2.Text = $"Average Food Cost: {recipeReport.AverageFoodCostPercentage:0.##}%";
                    _kpi3.Text = $"Cost Alerts (> {threshold}%): {recipeReport.Items.Count(i => i.IsCostAlert)}";
                    _kpi4.Text = "Target Margin: 65.00%";
                    break;

                case 1: // Ingredient Costs
                    var ingReport = await _mediator.Send(new GetIngredientCostReportQuery(warehouseId));
                    _ingredientGrid.DataSource = ingReport.Items;
                    _kpi1.Text = $"Total Ingredients: {ingReport.TotalIngredients}";
                    _kpi2.Text = $"Total Stock Valuation: {ingReport.TotalStockValuation:C2}";
                    _kpi3.Text = $"Multi-Recipe Ingredients: {ingReport.Items.Count(i => i.RecipesUsedInCount > 1)}";
                    _kpi4.Text = $"Active Cost Base: PKR";
                    break;

                case 2: // Stock Valuation
                    var valReport = await _mediator.Send(new GetInventoryValuationReportQuery(warehouseId));
                    _valuationGrid.DataSource = valReport.Items;
                    _kpi1.Text = $"Total Stock Items: {valReport.TotalItems}";
                    _kpi2.Text = $"Total Asset Valuation: {valReport.TotalAssetValuation:C2}";
                    _kpi3.Text = $"Low Stock Items: {valReport.LowStockCount}";
                    _kpi4.Text = $"Negative Stock Items: {valReport.NegativeStockCount}";
                    break;

                case 3: // Stock Variance
                    if (!warehouseId.HasValue)
                    {
                        // Auto-select first specific warehouse if "All" is selected
                        var firstSpecific = _warehousesByDisplay.Values.FirstOrDefault(v => v.HasValue);
                        if (firstSpecific.HasValue) warehouseId = firstSpecific.Value;
                    }

                    if (warehouseId.HasValue)
                    {
                        var varReport = await _mediator.Send(new GetStockVarianceReportQuery(warehouseId.Value, fromUtc, toUtc));
                        _varianceGrid.DataSource = varReport.Items;
                        _kpi1.Text = $"Items Evaluated: {varReport.TotalIngredientsEvaluated}";
                        _kpi2.Text = $"Theoretical Cost: {varReport.TotalTheoreticalCost:C2}";
                        _kpi3.Text = $"Actual Cost: {varReport.TotalActualCost:C2}";
                        _kpi4.Text = $"Variance Cost: {varReport.TotalVarianceCost:C2}";
                    }
                    else
                    {
                        _varianceGrid.DataSource = null;
                        _kpi1.Text = "Select a specific warehouse to view stock variance.";
                        _kpi2.Text = "-"; _kpi3.Text = "-"; _kpi4.Text = "-";
                    }
                    break;

                case 4: // Wastage
                    var wasteReport = await _mediator.Send(new GetWastageReportQuery(warehouseId, fromUtc, toUtc));
                    _wastageGrid.DataSource = wasteReport.Items;
                    _kpi1.Text = $"Total Spoilage Incidents: {wasteReport.TotalIncidents}";
                    _kpi2.Text = $"Total Wastage Qty: {wasteReport.TotalWastageQuantity:0.####}";
                    _kpi3.Text = $"Total Wastage Cost: {wasteReport.TotalWastageCost:C2}";
                    _kpi4.Text = "Reason: Prep & Spoilage";
                    break;
            }
        }
        catch (Exception ex)
        {
            XtraMessageBox.Show(this, $"Failed to load report: {ex.Message}", "Report Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
            _refreshButton.Enabled = true;
        }
    }

    private (GridControl Grid, string Title) GetActiveGridAndTitle() =>
        _tabControl.SelectedTabPageIndex switch
        {
            0 => (_recipeGrid, "Recipe_Food_Cost_Report"),
            1 => (_ingredientGrid, "Ingredient_Cost_Report"),
            2 => (_valuationGrid, "Inventory_Asset_Valuation_Report"),
            3 => (_varianceGrid, "Theoretical_vs_Actual_Stock_Variance_Report"),
            4 => (_wastageGrid, "Kitchen_Wastage_Report"),
            _ => (_recipeGrid, "Inventory_Report")
        };

    private void ExportActiveReportToExcel()
    {
        var (grid, title) = GetActiveGridAndTitle();
        GridReportingPrintService.ExportToExcel(grid, title, this);
    }

    private void ExportActiveReportToPdf()
    {
        var (grid, title) = GetActiveGridAndTitle();
        string periodText = $"{_fromDateEdit.DateTime:yyyy-MM-dd} to {_toDateEdit.DateTime:yyyy-MM-dd}";
        GridReportingPrintService.ExportToPdf(grid, title, periodText, this);
    }

    private void ShowActiveReportPrintPreview()
    {
        var (grid, title) = GetActiveGridAndTitle();
        string periodText = $"{_fromDateEdit.DateTime:yyyy-MM-dd} to {_toDateEdit.DateTime:yyyy-MM-dd}";
        GridReportingPrintService.ShowPreview(grid, title, periodText, this);
    }
}
