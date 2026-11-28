using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Repository;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraTab;

namespace Clovent.Desktop.Inventory.Recipes;

partial class RecipeEditForm
{
    private System.ComponentModel.IContainer components = null;

    private ComboBoxEdit _variantCombo;
    private TextEdit _nameEdit;
    private SpinEdit _yieldQtyEdit;
    private ComboBoxEdit _yieldUnitCombo;
    private TextEdit _notesEdit;
    private CheckEdit _isActiveCheck;
    private CheckEdit _newVersionCheck;
    private LabelControl _versionLabel;

    private XtraTabControl _tabControl;
    private XtraTabPage _ingredientsTabPage;
    private XtraTabPage _modifiersTabPage;
    private XtraTabPage _historyTabPage;

    private GridControl _ingredientsGrid;
    private GridView _ingredientsGridView;
    private RepositoryItemComboBox _uomRepositoryItem;
    private SimpleButton _addIngredientBtn;
    private SimpleButton _removeIngredientBtn;

    private GridControl _modifiersGrid;
    private GridView _modifiersGridView;
    private RepositoryItemComboBox _modifierUomRepositoryItem;
    private SimpleButton _addModifierBtn;
    private SimpleButton _removeModifierBtn;

    private GridControl _versionsGrid;
    private GridView _versionsGridView;

    private LabelControl _portionCostLabel;
    private LabelControl _sellingPriceLabel;
    private LabelControl _foodCostPctLabel;
    private LabelControl _grossMarginPctLabel;
    private LabelControl _costAlertBadge;

    private SimpleButton _saveButton;
    private SimpleButton _cancelButton;

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

        ClientSize = new Size(880, 720);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
        MinimumSize = new Size(780, 600);

        var rootLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(12),
            Margin = new Padding(0)
        };
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // General Fields
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Tabs
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Theoretical Costing Summary
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Disclaimer
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Buttons

        // 1. General Fields Header
        var headerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 4,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 10)
        };
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        // Row 0: Menu Item & Recipe Name
        headerLayout.Controls.Add(new LabelControl { Text = "Menu Item:", Dock = DockStyle.Fill, Appearance = { TextOptions = { VAlignment = DevExpress.Utils.VertAlignment.Center } } }, 0, 0);
        _variantCombo = new ComboBoxEdit { Dock = DockStyle.Fill };
        headerLayout.Controls.Add(_variantCombo, 1, 0);

        headerLayout.Controls.Add(new LabelControl { Text = "Recipe Name:", Dock = DockStyle.Fill, Appearance = { TextOptions = { VAlignment = DevExpress.Utils.VertAlignment.Center } } }, 2, 0);
        _nameEdit = new TextEdit { Dock = DockStyle.Fill };
        headerLayout.Controls.Add(_nameEdit, 3, 0);

        // Row 1: Yield Qty & Yield Unit
        headerLayout.Controls.Add(new LabelControl { Text = "Yield Quantity:", Dock = DockStyle.Fill, Appearance = { TextOptions = { VAlignment = DevExpress.Utils.VertAlignment.Center } } }, 0, 1);
        _yieldQtyEdit = new SpinEdit { Dock = DockStyle.Fill };
        _yieldQtyEdit.Properties.MinValue = 0.0001m;
        _yieldQtyEdit.Properties.MaxValue = 100000m;
        _yieldQtyEdit.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
        _yieldQtyEdit.Properties.DisplayFormat.FormatString = "0.####";
        _yieldQtyEdit.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
        _yieldQtyEdit.Properties.EditFormat.FormatString = "0.####";
        headerLayout.Controls.Add(_yieldQtyEdit, 1, 1);

        headerLayout.Controls.Add(new LabelControl { Text = "Yield Unit:", Dock = DockStyle.Fill, Appearance = { TextOptions = { VAlignment = DevExpress.Utils.VertAlignment.Center } } }, 2, 1);
        _yieldUnitCombo = new ComboBoxEdit { Dock = DockStyle.Fill };
        _yieldUnitCombo.Properties.Items.AddRange(new object[] { "Portion", "Item", "Serving", "Plate", "Batch", "Cup", "Bowl", "Slice", "Dozen" });
        headerLayout.Controls.Add(_yieldUnitCombo, 3, 1);

        // Row 2: Preparation Notes
        headerLayout.Controls.Add(new LabelControl { Text = "Prep Notes:", Dock = DockStyle.Fill, Appearance = { TextOptions = { VAlignment = DevExpress.Utils.VertAlignment.Center } } }, 0, 2);
        _notesEdit = new TextEdit { Dock = DockStyle.Fill };
        headerLayout.Controls.Add(_notesEdit, 1, 2);

        // Version & Status
        var statusPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
        _isActiveCheck = new CheckEdit { Text = "Active Recipe", AutoSizeInLayoutControl = true };
        _versionLabel = new LabelControl { Text = "Version 1", Appearance = { Font = new Font("Segoe UI", 9F, FontStyle.Bold) }, Padding = new Padding(8, 2, 0, 0) };
        statusPanel.Controls.Add(_isActiveCheck);
        statusPanel.Controls.Add(_versionLabel);
        headerLayout.Controls.Add(statusPanel, 3, 2);

        // Row 3: New Version Checkbox
        _newVersionCheck = new CheckEdit { Text = "Save as New Version Snapshot", Visible = false, Dock = DockStyle.Fill };
        headerLayout.Controls.Add(_newVersionCheck, 1, 3);
        headerLayout.SetColumnSpan(_newVersionCheck, 3);

        rootLayout.Controls.Add(headerLayout, 0, 0);

        // 2. Tabs
        _tabControl = new XtraTabControl { Dock = DockStyle.Fill };
        _ingredientsTabPage = new XtraTabPage { Text = "Ingredients & Bill of Materials" };
        _modifiersTabPage = new XtraTabPage { Text = "Recipe Modifiers" };
        _historyTabPage = new XtraTabPage { Text = "Version History" };

        _tabControl.TabPages.Add(_ingredientsTabPage);
        _tabControl.TabPages.Add(_modifiersTabPage);
        _tabControl.TabPages.Add(_historyTabPage);

        // --- Setup Ingredients Tab ---
        var ingTabPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        ingTabPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        ingTabPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var ingToolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0, 0, 0, 6) };
        _addIngredientBtn = new SimpleButton { Text = "+ Add Ingredient", Size = new Size(130, 30) };
        _removeIngredientBtn = new SimpleButton { Text = "Remove Selected", Size = new Size(130, 30) };
        ingToolbar.Controls.Add(_addIngredientBtn);
        ingToolbar.Controls.Add(_removeIngredientBtn);
        ingTabPanel.Controls.Add(ingToolbar, 0, 0);

        _ingredientsGrid = new GridControl { Dock = DockStyle.Fill };
        _ingredientsGridView = new GridView(_ingredientsGrid);
        _ingredientsGrid.MainView = _ingredientsGridView;
        _ingredientsGridView.OptionsView.ShowGroupPanel = false;
        _ingredientsGridView.OptionsView.ShowIndicator = false;
        _ingredientsGridView.RowHeight = 28;

        var colIngName = _ingredientsGridView.Columns.AddVisible("IngredientName", "Ingredient");
        colIngName.Width = 220;
        var colIngQty = _ingredientsGridView.Columns.AddVisible("Quantity", "Net Qty");
        colIngQty.Width = 90;
        colIngQty.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
        colIngQty.DisplayFormat.FormatString = "0.####";
        var colIngUom = _ingredientsGridView.Columns.AddVisible("UnitOfMeasureCode", "Unit");
        colIngUom.Width = 70;
        _uomRepositoryItem = new RepositoryItemComboBox();
        _ingredientsGrid.RepositoryItems.Add(_uomRepositoryItem);
        colIngUom.ColumnEdit = _uomRepositoryItem;
        var colIngWaste = _ingredientsGridView.Columns.AddVisible("WastagePercentage", "Waste %");
        colIngWaste.Width = 80;
        colIngWaste.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
        colIngWaste.DisplayFormat.FormatString = "0.##";
        var colGrossQty = _ingredientsGridView.Columns.AddVisible("GrossQuantityRequired", "Gross Qty");
        colGrossQty.Width = 90;
        colGrossQty.OptionsColumn.AllowEdit = false;
        colGrossQty.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
        colGrossQty.DisplayFormat.FormatString = "0.####";
        var colCostOverride = _ingredientsGridView.Columns.AddVisible("CostPerUnitOverride", "Cost Override");
        colCostOverride.Width = 100;
        colCostOverride.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
        colCostOverride.DisplayFormat.FormatString = "C2";

        ingTabPanel.Controls.Add(_ingredientsGrid, 0, 1);
        _ingredientsTabPage.Controls.Add(ingTabPanel);

        // --- Setup Modifiers Tab ---
        var modTabPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        modTabPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        modTabPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var modToolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0, 0, 0, 6) };
        _addModifierBtn = new SimpleButton { Text = "+ Add Modifier", Size = new Size(130, 30) };
        _removeModifierBtn = new SimpleButton { Text = "Remove Selected", Size = new Size(130, 30) };
        modToolbar.Controls.Add(_addModifierBtn);
        modToolbar.Controls.Add(_removeModifierBtn);
        modTabPanel.Controls.Add(modToolbar, 0, 0);

        _modifiersGrid = new GridControl { Dock = DockStyle.Fill };
        _modifiersGridView = new GridView(_modifiersGrid);
        _modifiersGrid.MainView = _modifiersGridView;
        _modifiersGridView.OptionsView.ShowGroupPanel = false;
        _modifiersGridView.OptionsView.ShowIndicator = false;
        _modifiersGridView.RowHeight = 28;

        var colModName = _modifiersGridView.Columns.AddVisible("ModifierName", "Modifier Name");
        colModName.Width = 180;
        var colModIng = _modifiersGridView.Columns.AddVisible("IngredientName", "Ingredient Item");
        colModIng.Width = 180;
        var colModQty = _modifiersGridView.Columns.AddVisible("Quantity", "Quantity");
        colModQty.Width = 80;
        colModQty.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
        colModQty.DisplayFormat.FormatString = "0.####";
        var colModUom = _modifiersGridView.Columns.AddVisible("UnitOfMeasureCode", "Unit");
        colModUom.Width = 70;
        _modifierUomRepositoryItem = new RepositoryItemComboBox();
        _modifiersGrid.RepositoryItems.Add(_modifierUomRepositoryItem);
        colModUom.ColumnEdit = _modifierUomRepositoryItem;
        var colModWaste = _modifiersGridView.Columns.AddVisible("WastagePercentage", "Waste %");
        colModWaste.Width = 80;
        colModWaste.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
        colModWaste.DisplayFormat.FormatString = "0.##";

        modTabPanel.Controls.Add(_modifiersGrid, 0, 1);
        _modifiersTabPage.Controls.Add(modTabPanel);

        // --- Setup Version History Tab ---
        _versionsGrid = new GridControl { Dock = DockStyle.Fill };
        _versionsGridView = new GridView(_versionsGrid);
        _versionsGrid.MainView = _versionsGridView;
        _versionsGridView.OptionsBehavior.Editable = false;
        _versionsGridView.OptionsView.ShowGroupPanel = false;
        _versionsGridView.OptionsView.ShowIndicator = false;
        _versionsGridView.RowHeight = 28;

        var colVer = _versionsGridView.Columns.AddVisible("Version", "Version");
        colVer.Width = 80;
        var colVerDate = _versionsGridView.Columns.AddVisible("EffectiveDate", "Effective Date");
        colVerDate.Width = 160;
        var colVerYield = _versionsGridView.Columns.AddVisible("Yield", "Yield");
        colVerYield.Width = 120;
        var colVerIngCount = _versionsGridView.Columns.AddVisible("IngredientCount", "Ingredients");
        colVerIngCount.Width = 100;

        _historyTabPage.Controls.Add(_versionsGrid);

        rootLayout.Controls.Add(_tabControl, 0, 1);

        // 3. Theoretical Costing Summary Panel
        var costPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 2,
            Margin = new Padding(0, 8, 0, 4),
            BackColor = Color.FromArgb(248, 250, 252),
            Padding = new Padding(8)
        };
        for (int i = 0; i < 5; i++)
            costPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));

        costPanel.Controls.Add(CreateHeaderLabel("Cost / Portion"), 0, 0);
        costPanel.Controls.Add(CreateHeaderLabel("Selling Price"), 1, 0);
        costPanel.Controls.Add(CreateHeaderLabel("Food Cost %"), 2, 0);
        costPanel.Controls.Add(CreateHeaderLabel("Gross Margin %"), 3, 0);
        costPanel.Controls.Add(CreateHeaderLabel("Status"), 4, 0);

        _portionCostLabel = CreateValueLabel("0.00");
        _sellingPriceLabel = CreateValueLabel("0.00");
        _foodCostPctLabel = CreateValueLabel("0.00%");
        _grossMarginPctLabel = CreateValueLabel("0.00%");

        _costAlertBadge = new LabelControl
        {
            Text = "ALERT: > 35%",
            Visible = false,
            Appearance = { Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.White, BackColor = Color.FromArgb(220, 38, 38) },
            Padding = new Padding(4, 2, 4, 2),
            Dock = DockStyle.Fill
        };

        costPanel.Controls.Add(_portionCostLabel, 0, 1);
        costPanel.Controls.Add(_sellingPriceLabel, 1, 1);
        costPanel.Controls.Add(_foodCostPctLabel, 2, 1);
        costPanel.Controls.Add(_grossMarginPctLabel, 3, 1);
        costPanel.Controls.Add(_costAlertBadge, 4, 1);

        rootLayout.Controls.Add(costPanel, 0, 2);

        // 4. Disclaimer
        var disclaimer = new LabelControl
        {
            Dock = DockStyle.Fill,
            Text = "Theoretical Food Cost: Calculated from standard BOM formulas. Excludes shrinkage, scrap, and cashier variances. Realized business profit is verified by the general ledger and cash drawer reconciliations.",
            Appearance = { Font = new Font("Segoe UI", 8.25F, FontStyle.Italic), ForeColor = Color.FromArgb(100, 116, 139) },
            Margin = new Padding(0, 4, 0, 6)
        };
        rootLayout.Controls.Add(disclaimer, 0, 3);

        // 5. Button Panel
        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Margin = new Padding(0)
        };
        _cancelButton = new SimpleButton { Text = "Cancel", Size = new Size(95, 34) };
        _saveButton = new SimpleButton { Text = "Save Recipe", Size = new Size(110, 34), Appearance = { Font = new Font("Segoe UI", 9F, FontStyle.Bold) } };

        btnPanel.Controls.Add(_cancelButton);
        btnPanel.Controls.Add(_saveButton);
        rootLayout.Controls.Add(btnPanel, 0, 4);

        Controls.Add(rootLayout);
    }

    private static LabelControl CreateHeaderLabel(string text) =>
        new()
        {
            Text = text,
            Dock = DockStyle.Fill,
            Appearance = { Font = new Font("Segoe UI", 8F, FontStyle.Regular), ForeColor = Color.FromArgb(100, 116, 139), TextOptions = { HAlignment = DevExpress.Utils.HorzAlignment.Center } }
        };

    private static LabelControl CreateValueLabel(string text) =>
        new()
        {
            Text = text,
            Dock = DockStyle.Fill,
            Appearance = { Font = new Font("Segoe UI", 10F, FontStyle.Bold), TextOptions = { HAlignment = DevExpress.Utils.HorzAlignment.Center } }
        };
}
