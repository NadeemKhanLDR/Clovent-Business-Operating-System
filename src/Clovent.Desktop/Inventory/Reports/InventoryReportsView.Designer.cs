using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraTab;

namespace Clovent.Desktop.Inventory.Reports;

partial class InventoryReportsView
{
    private System.ComponentModel.IContainer components = null;

    private ComboBoxEdit _warehouseCombo;
    private ComboBoxEdit _periodCombo;
    private DateEdit _fromDateEdit;
    private DateEdit _toDateEdit;
    private SpinEdit _alertThresholdEdit;
    private SimpleButton _refreshButton;
    private SimpleButton _exportExcelBtn;
    private SimpleButton _exportPdfBtn;
    private SimpleButton _printPreviewBtn;

    private LabelControl _kpi1;
    private LabelControl _kpi2;
    private LabelControl _kpi3;
    private LabelControl _kpi4;

    private XtraTabControl _tabControl;
    private XtraTabPage _tabRecipeCost;
    private XtraTabPage _tabIngredientCost;
    private XtraTabPage _tabValuation;
    private XtraTabPage _tabVariance;
    private XtraTabPage _tabWastage;

    private GridControl _recipeGrid;
    private GridView _recipeGridView;

    private GridControl _ingredientGrid;
    private GridView _ingredientGridView;

    private GridControl _valuationGrid;
    private GridView _valuationGridView;

    private GridControl _varianceGrid;
    private GridView _varianceGridView;

    private GridControl _wastageGrid;
    private GridView _wastageGridView;

    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;

        var rootLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(10),
            Margin = new Padding(0)
        };
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Filter toolbar
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // KPI strip
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Tabbed grids
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Disclaimer

        // 1. Filter Toolbar
        var filterPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0, 0, 0, 8)
        };

        filterPanel.Controls.Add(new LabelControl { Text = "Warehouse:", Padding = new Padding(0, 6, 4, 0) });
        _warehouseCombo = new ComboBoxEdit { Width = 150 };
        filterPanel.Controls.Add(_warehouseCombo);

        filterPanel.Controls.Add(new LabelControl { Text = "Period:", Padding = new Padding(8, 6, 4, 0) });
        _periodCombo = new ComboBoxEdit { Width = 110 };
        filterPanel.Controls.Add(_periodCombo);

        filterPanel.Controls.Add(new LabelControl { Text = "From:", Padding = new Padding(8, 6, 4, 0) });
        _fromDateEdit = new DateEdit { Width = 100 };
        filterPanel.Controls.Add(_fromDateEdit);

        filterPanel.Controls.Add(new LabelControl { Text = "To:", Padding = new Padding(8, 6, 4, 0) });
        _toDateEdit = new DateEdit { Width = 100 };
        filterPanel.Controls.Add(_toDateEdit);

        filterPanel.Controls.Add(new LabelControl { Text = "Cost Alert %:", Padding = new Padding(8, 6, 4, 0) });
        _alertThresholdEdit = new SpinEdit { Width = 65, Value = 35m };
        _alertThresholdEdit.Properties.MinValue = 1m;
        _alertThresholdEdit.Properties.MaxValue = 100m;
        filterPanel.Controls.Add(_alertThresholdEdit);

        _refreshButton = new SimpleButton { Text = "Refresh", Width = 80, Appearance = { Font = new Font("Segoe UI", 9F, FontStyle.Bold) } };
        _exportExcelBtn = new SimpleButton { Text = "Excel", Width = 70 };
        _exportPdfBtn = new SimpleButton { Text = "PDF", Width = 65 };
        _printPreviewBtn = new SimpleButton { Text = "Print", Width = 65 };

        filterPanel.Controls.Add(_refreshButton);
        filterPanel.Controls.Add(_exportExcelBtn);
        filterPanel.Controls.Add(_exportPdfBtn);
        filterPanel.Controls.Add(_printPreviewBtn);

        rootLayout.Controls.Add(filterPanel, 0, 0);

        // 2. KPI Summary Strip
        var kpiPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8),
            BackColor = Color.FromArgb(241, 245, 249),
            Padding = new Padding(8, 6, 8, 6)
        };
        for (int i = 0; i < 4; i++)
            kpiPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

        _kpi1 = CreateKpiLabel("Loading...");
        _kpi2 = CreateKpiLabel("-");
        _kpi3 = CreateKpiLabel("-");
        _kpi4 = CreateKpiLabel("-");

        kpiPanel.Controls.Add(_kpi1, 0, 0);
        kpiPanel.Controls.Add(_kpi2, 1, 0);
        kpiPanel.Controls.Add(_kpi3, 2, 0);
        kpiPanel.Controls.Add(_kpi4, 3, 0);

        rootLayout.Controls.Add(kpiPanel, 0, 1);

        // 3. TabControl & Grids
        _tabControl = new XtraTabControl { Dock = DockStyle.Fill };
        _tabRecipeCost = new XtraTabPage { Text = "Recipe Food Cost & Margins" };
        _tabIngredientCost = new XtraTabPage { Text = "Ingredient Purchasing Costs" };
        _tabValuation = new XtraTabPage { Text = "Inventory Asset Valuation" };
        _tabVariance = new XtraTabPage { Text = "Theoretical vs Actual Stock Variance" };
        _tabWastage = new XtraTabPage { Text = "Kitchen Spoilage & Wastage" };

        _tabControl.TabPages.AddRange([_tabRecipeCost, _tabIngredientCost, _tabValuation, _tabVariance, _tabWastage]);

        // Setup Recipe Cost Grid
        _recipeGrid = new GridControl { Dock = DockStyle.Fill };
        _recipeGridView = new GridView(_recipeGrid);
        _recipeGrid.MainView = _recipeGridView;
        ConfigureGridView(_recipeGridView);
        _recipeGridView.Columns.AddVisible("RecipeName", "Recipe Name").Width = 200;
        _recipeGridView.Columns.AddVisible("YieldQuantity", "Yield Qty").Width = 70;
        _recipeGridView.Columns.AddVisible("YieldUnit", "Yield Unit").Width = 70;
        AddCurrencyColumn(_recipeGridView, "CostPerPortion", "Cost / Portion", 100);
        AddCurrencyColumn(_recipeGridView, "SellingPrice", "Selling Price", 100);
        AddPercentColumn(_recipeGridView, "FoodCostPercentage", "Food Cost %", 90);
        AddCurrencyColumn(_recipeGridView, "GrossProfitPerPortion", "Gross Profit", 100);
        AddPercentColumn(_recipeGridView, "GrossMarginPercentage", "Gross Margin %", 95);
        _recipeGridView.Columns.AddVisible("IsCostAlert", "Alert (> 35%)").Width = 85;
        _tabRecipeCost.Controls.Add(_recipeGrid);

        // Setup Ingredient Cost Grid
        _ingredientGrid = new GridControl { Dock = DockStyle.Fill };
        _ingredientGridView = new GridView(_ingredientGrid);
        _ingredientGrid.MainView = _ingredientGridView;
        ConfigureGridView(_ingredientGridView);
        _ingredientGridView.Columns.AddVisible("IngredientName", "Ingredient").Width = 220;
        _ingredientGridView.Columns.AddVisible("Sku", "SKU").Width = 100;
        _ingredientGridView.Columns.AddVisible("UnitOfMeasureCode", "Unit").Width = 70;
        AddCurrencyColumn(_ingredientGridView, "CurrentUnitCost", "Unit Cost", 110);
        AddDecimalColumn(_ingredientGridView, "QuantityOnHand", "On-Hand Qty", 100);
        AddCurrencyColumn(_ingredientGridView, "TotalStockValuation", "Stock Valuation", 120);
        _ingredientGridView.Columns.AddVisible("RecipesUsedInCount", "Recipes Used In").Width = 110;
        _tabIngredientCost.Controls.Add(_ingredientGrid);

        // Setup Valuation Grid
        _valuationGrid = new GridControl { Dock = DockStyle.Fill };
        _valuationGridView = new GridView(_valuationGrid);
        _valuationGrid.MainView = _valuationGridView;
        ConfigureGridView(_valuationGridView);
        _valuationGridView.Columns.AddVisible("ProductName", "Item Name").Width = 220;
        _valuationGridView.Columns.AddVisible("Sku", "SKU").Width = 100;
        _valuationGridView.Columns.AddVisible("UnitOfMeasureCode", "Unit").Width = 70;
        AddDecimalColumn(_valuationGridView, "QuantityOnHand", "On-Hand Qty", 100);
        AddCurrencyColumn(_valuationGridView, "UnitCost", "Unit Cost", 100);
        AddCurrencyColumn(_valuationGridView, "TotalValuation", "Total Valuation", 120);
        AddDecimalColumn(_valuationGridView, "MinimumStock", "Min Stock", 90);
        _valuationGridView.Columns.AddVisible("IsLowStock", "Low Stock Alert").Width = 95;
        _valuationGridView.Columns.AddVisible("IsNegativeStock", "Negative Alert").Width = 95;
        _tabValuation.Controls.Add(_valuationGrid);

        // Setup Variance Grid
        _varianceGrid = new GridControl { Dock = DockStyle.Fill };
        _varianceGridView = new GridView(_varianceGrid);
        _varianceGrid.MainView = _varianceGridView;
        ConfigureGridView(_varianceGridView);
        _varianceGridView.Columns.AddVisible("IngredientName", "Ingredient").Width = 200;
        _varianceGridView.Columns.AddVisible("Sku", "SKU").Width = 90;
        _varianceGridView.Columns.AddVisible("UnitOfMeasureCode", "Unit").Width = 60;
        AddDecimalColumn(_varianceGridView, "TheoreticalQuantity", "Theoretical Qty", 110);
        AddDecimalColumn(_varianceGridView, "ActualQuantity", "Actual Qty", 100);
        AddDecimalColumn(_varianceGridView, "VarianceQuantity", "Variance Qty", 100);
        AddCurrencyColumn(_varianceGridView, "UnitCost", "Unit Cost", 90);
        AddCurrencyColumn(_varianceGridView, "VarianceCost", "Variance Cost", 100);
        _varianceGridView.Columns.AddVisible("VarianceClassification", "Classification").Width = 120;
        _tabVariance.Controls.Add(_varianceGrid);

        // Setup Wastage Grid
        _wastageGrid = new GridControl { Dock = DockStyle.Fill };
        _wastageGridView = new GridView(_wastageGrid);
        _wastageGrid.MainView = _wastageGridView;
        ConfigureGridView(_wastageGridView);
        var colDate = _wastageGridView.Columns.AddVisible("OccurredAtUtc", "Occurred (UTC)");
        colDate.Width = 140;
        colDate.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
        colDate.DisplayFormat.FormatString = "yyyy-MM-dd HH:mm";
        _wastageGridView.Columns.AddVisible("ProductName", "Item Name").Width = 200;
        _wastageGridView.Columns.AddVisible("Sku", "SKU").Width = 90;
        AddDecimalColumn(_wastageGridView, "Quantity", "Wastage Qty", 100);
        _wastageGridView.Columns.AddVisible("UnitOfMeasureCode", "Unit").Width = 60;
        AddCurrencyColumn(_wastageGridView, "EstimatedUnitCost", "Unit Cost", 90);
        AddCurrencyColumn(_wastageGridView, "EstimatedWastageCost", "Wastage Cost", 110);
        _wastageGridView.Columns.AddVisible("Reason", "Reason / Notes").Width = 180;
        _tabWastage.Controls.Add(_wastageGrid);

        rootLayout.Controls.Add(_tabControl, 0, 2);

        // 4. Disclaimer
        var disclaimerLabel = new LabelControl
        {
            Dock = DockStyle.Fill,
            Text = "Theoretical Food Costing Notice: Recipe food cost percentages and gross margins are calculated from standard bill-of-materials formulas and target yields under ideal kitchen execution. Realized business profit is strictly derived from the double-entry financial ledger, verified vendor purchase invoices, and physical cash drawer reconciliations.",
            Appearance = { Font = new Font("Segoe UI", 8.25F, FontStyle.Italic), ForeColor = Color.FromArgb(100, 116, 139) },
            Margin = new Padding(0, 6, 0, 2),
            AutoSizeMode = LabelAutoSizeMode.Vertical
        };
        rootLayout.Controls.Add(disclaimerLabel, 0, 3);

        Controls.Add(rootLayout);
    }

    private static void ConfigureGridView(GridView view)
    {
        view.OptionsBehavior.Editable = false;
        view.OptionsView.ShowGroupPanel = false;
        view.OptionsView.ShowIndicator = false;
        view.RowHeight = 28;
    }

    private static void AddCurrencyColumn(GridView view, string field, string caption, int width)
    {
        var col = view.Columns.AddVisible(field, caption);
        col.Width = width;
        col.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
        col.DisplayFormat.FormatString = "C2";
    }

    private static void AddDecimalColumn(GridView view, string field, string caption, int width)
    {
        var col = view.Columns.AddVisible(field, caption);
        col.Width = width;
        col.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
        col.DisplayFormat.FormatString = "0.####";
    }

    private static void AddPercentColumn(GridView view, string field, string caption, int width)
    {
        var col = view.Columns.AddVisible(field, caption);
        col.Width = width;
        col.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
        col.DisplayFormat.FormatString = "0.##'%'";
    }

    private static LabelControl CreateKpiLabel(string text) =>
        new()
        {
            Text = text,
            Dock = DockStyle.Fill,
            Appearance = { Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(30, 41, 59) }
        };
}
