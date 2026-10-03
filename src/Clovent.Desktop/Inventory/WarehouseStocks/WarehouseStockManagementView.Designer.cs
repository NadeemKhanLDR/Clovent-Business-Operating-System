using Clovent.Desktop.MasterData;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Inventory.WarehouseStocks;

partial class WarehouseStockManagementView
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    private EntityPicker _warehousePicker;
    private SimpleButton _receiveInventoryButton;
    private MasterDataListView<WarehouseStockRow> _listView;

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify the contents of
    /// this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;

        _warehousePicker = new EntityPicker("Warehouse:", comboWidth: 260, customPadding: new Padding(12, 12, 8, 8));
        _receiveInventoryButton = new SimpleButton
        {
            Text = "Receive Inventory",
            AutoSize = true,
            MinimumSize = new Size(130, 28),
            Padding = new Padding(10, 4, 10, 4),
            Margin = new Padding(12, 0, 0, 0),
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Left
        };
        _receiveInventoryButton.Appearance.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _receiveInventoryButton.Appearance.Options.UseFont = true;

        _listView = new MasterDataListView<WarehouseStockRow>(
        [
            new MasterDataColumn("Sku", "SKU", 110),
            new MasterDataColumn("Name", "Product", 240),
            new MasterDataColumn("QuantityOnHand", "On Hand", 85),
            new MasterDataColumn("QuantityReserved", "Reserved", 85),
            new MasterDataColumn("QuantityAvailable", "Available", 85),
            new MasterDataColumn("MinimumStock", "Min", 65),
            new MasterDataColumn("MaximumStock", "Max", 65),
            new MasterDataColumn("AllowNegativeStock", "Neg. OK", 65),
            new MasterDataColumn("UpdatedAtUtc", "Updated (UTC)", 160),
        ],
        [
            new MasterDataListAction<WarehouseStockRow>("Receive", ReceiveAsync, FeatureOperation: "receive"),
            new MasterDataListAction<WarehouseStockRow>("Issue", IssueAsync, FeatureOperation: "issue"),
            new MasterDataListAction<WarehouseStockRow>("Reserve", ReserveAsync, FeatureOperation: "reserve"),
            new MasterDataListAction<WarehouseStockRow>("Release", ReleaseAsync, FeatureOperation: "release"),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = row => $"{row.Sku} {row.Name}",
            CanUseFeatureAsync = operation => CanUseFeatureAsync(operation),
            OnNew = CreateAsync,
            OnEdit = EditAsync,
        };

        _warehousePicker.SelectionChanged += WarehousePicker_SelectionChanged;
        _receiveInventoryButton.Click += ReceiveInventoryButton_Click;

        var headerPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 1,
            Padding = Padding.Empty,
            Margin = Padding.Empty
        };
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        headerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _warehousePicker.Dock = DockStyle.None;
        _warehousePicker.Anchor = AnchorStyles.Left;
        headerPanel.Controls.Add(_warehousePicker, 0, 0);
        headerPanel.Controls.Add(_receiveInventoryButton, 1, 0);

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        mainLayout.Controls.Add(headerPanel, 0, 0);
        mainLayout.Controls.Add(new Clovent.Desktop.Forms.Base.GridSpacer(), 0, 1);
        _listView.Dock = DockStyle.Fill;
        mainLayout.Controls.Add(_listView, 0, 2);

        Controls.Add(mainLayout);
        Load += WarehouseStockManagementView_Load;
    }

    #endregion
}
