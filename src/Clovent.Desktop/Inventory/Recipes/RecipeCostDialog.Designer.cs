using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;

namespace Clovent.Desktop.Inventory.Recipes;

partial class RecipeCostDialog
{
    private System.ComponentModel.IContainer components = null;

    private LabelControl _recipeNameLabel;
    private LabelControl _yieldLabel;
    private LabelControl _alertBadge;
    private LabelControl _batchCostValue;
    private LabelControl _portionCostValue;
    private LabelControl _sellingPriceValue;
    private LabelControl _foodCostValue;
    private LabelControl _grossMarginValue;
    private GridControl _gridControl;
    private GridView _gridView;
    private SimpleButton _closeButton;

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        ClientSize = new Size(750, 560);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(16),
            Margin = new Padding(0)
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Header
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // KPI Panel
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Grid
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Disclaimer
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Buttons

        // 1. Header
        var headerPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            Margin = new Padding(0, 0, 0, 10)
        };
        _recipeNameLabel = new LabelControl
        {
            Text = "Recipe Name",
            Appearance = { Font = new Font("Segoe UI", 13F, FontStyle.Bold) }
        };
        _yieldLabel = new LabelControl
        {
            Text = "Yield: 1.0000 Portion",
            Appearance = { Font = new Font("Segoe UI", 9.5F, FontStyle.Regular), ForeColor = Color.Gray }
        };
        _alertBadge = new LabelControl
        {
            Text = "HIGH FOOD COST WARNING",
            Visible = false,
            Appearance = { Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.FromArgb(220, 38, 38) },
            Padding = new Padding(6, 3, 6, 3)
        };
        headerPanel.Controls.Add(_recipeNameLabel);
        headerPanel.Controls.Add(_yieldLabel);
        headerPanel.Controls.Add(_alertBadge);
        mainLayout.Controls.Add(headerPanel, 0, 0);

        // 2. KPI Summary Panel
        var kpiLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 2,
            Margin = new Padding(0, 0, 0, 12),
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(10)
        };
        for (int i = 0; i < 5; i++)
            kpiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));

        kpiLayout.Controls.Add(CreateHeaderLabel("Batch Cost"), 0, 0);
        kpiLayout.Controls.Add(CreateHeaderLabel("Cost / Portion"), 1, 0);
        kpiLayout.Controls.Add(CreateHeaderLabel("Selling Price"), 2, 0);
        kpiLayout.Controls.Add(CreateHeaderLabel("Food Cost %"), 3, 0);
        kpiLayout.Controls.Add(CreateHeaderLabel("Gross Margin %"), 4, 0);

        _batchCostValue = CreateValueLabel("0.00");
        _portionCostValue = CreateValueLabel("0.00");
        _sellingPriceValue = CreateValueLabel("0.00");
        _foodCostValue = CreateValueLabel("0.00%");
        _grossMarginValue = CreateValueLabel("0.00%");

        kpiLayout.Controls.Add(_batchCostValue, 0, 1);
        kpiLayout.Controls.Add(_portionCostValue, 1, 1);
        kpiLayout.Controls.Add(_sellingPriceValue, 2, 1);
        kpiLayout.Controls.Add(_foodCostValue, 3, 1);
        kpiLayout.Controls.Add(_grossMarginValue, 4, 1);

        mainLayout.Controls.Add(kpiLayout, 0, 1);

        // 3. Grid
        _gridControl = new GridControl { Dock = DockStyle.Fill };
        _gridView = new GridView(_gridControl);
        _gridControl.MainView = _gridView;
        _gridView.OptionsBehavior.Editable = false;
        _gridView.OptionsView.ShowGroupPanel = false;
        _gridView.OptionsView.ShowIndicator = false;
        _gridView.RowHeight = 28;

        var colName = _gridView.Columns.AddVisible("IngredientName", "Ingredient");
        colName.Width = 200;
        var colQty = _gridView.Columns.AddVisible("GrossQuantity", "Gross Quantity");
        colQty.Width = 100;
        colQty.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
        colQty.DisplayFormat.FormatString = "0.####";
        var colUnit = _gridView.Columns.AddVisible("Unit", "Unit");
        colUnit.Width = 60;
        var colUnitCost = _gridView.Columns.AddVisible("UnitCost", "Unit Cost");
        colUnitCost.Width = 100;
        colUnitCost.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
        colUnitCost.DisplayFormat.FormatString = "C2";
        var colTotal = _gridView.Columns.AddVisible("TotalCost", "Line Cost");
        colTotal.Width = 110;
        colTotal.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
        colTotal.DisplayFormat.FormatString = "C2";
        var colPct = _gridView.Columns.AddVisible("CostPercentage", "% of Batch");
        colPct.Width = 90;
        colPct.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
        colPct.DisplayFormat.FormatString = "0.##'%'";

        mainLayout.Controls.Add(_gridControl, 0, 2);

        // 4. Disclaimer
        var disclaimerLabel = new LabelControl
        {
            Dock = DockStyle.Fill,
            Text = "Note: Theoretical costing is calculated from standard recipe bill-of-materials formulas and current purchasing costs. Actual business profit is verified by the general ledger and cash drawer reconciliations.",
            Appearance = { Font = new Font("Segoe UI", 8.5F, FontStyle.Italic), ForeColor = Color.FromArgb(100, 116, 139) },
            Margin = new Padding(0, 8, 0, 10),
            AutoSizeMode = LabelAutoSizeMode.Vertical
        };
        mainLayout.Controls.Add(disclaimerLabel, 0, 3);

        // 5. Button
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Margin = new Padding(0)
        };
        _closeButton = new SimpleButton
        {
            Text = "Close",
            DialogResult = DialogResult.OK,
            Size = new Size(100, 34)
        };
        _closeButton.Click += (s, e) => Close();
        buttonPanel.Controls.Add(_closeButton);
        mainLayout.Controls.Add(buttonPanel, 0, 4);

        Controls.Add(mainLayout);
    }

    private static LabelControl CreateHeaderLabel(string text) =>
        new()
        {
            Text = text,
            Dock = DockStyle.Fill,
            Appearance = { Font = new Font("Segoe UI", 8.5F, FontStyle.Regular), ForeColor = Color.FromArgb(100, 116, 139), TextOptions = { HAlignment = DevExpress.Utils.HorzAlignment.Center } }
        };

    private static LabelControl CreateValueLabel(string text) =>
        new()
        {
            Text = text,
            Dock = DockStyle.Fill,
            Appearance = { Font = new Font("Segoe UI", 11F, FontStyle.Bold), TextOptions = { HAlignment = DevExpress.Utils.HorzAlignment.Center } }
        };
}
