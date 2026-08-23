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

        _warehousePicker = new EntityPicker("Warehouse:");
        _receiveInventoryButton = new SimpleButton { Text = "Receive Inventory" };

        _listView = new MasterDataListView<WarehouseStockRow>(
        [
            new MasterDataColumn("Sku", "SKU", 100),
            new MasterDataColumn("Name", "Product", 180),
            new MasterDataColumn("QuantityOnHand", "On Hand", 80),
            new MasterDataColumn("QuantityReserved", "Reserved", 80),
            new MasterDataColumn("QuantityAvailable", "Available", 80),
            new MasterDataColumn("MinimumStock", "Min", 60),
            new MasterDataColumn("MaximumStock", "Max", 60),
            new MasterDataColumn("AllowNegativeStock", "Neg. OK", 60),
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

        // AutoSize toolbar, not a fixed Height=32 - with no AutoScaleMode in
        // this app, a DPI-grown button is taller than 32 logical px and got
        // clipped by the fixed panel.
        var toolbar = new PanelControl { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(0, 4, 0, 4) };
        var toolbarLayout = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        toolbarLayout.Controls.Add(_receiveInventoryButton);
        toolbar.Controls.Add(toolbarLayout);

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _warehousePicker.Dock = DockStyle.Top;
        mainLayout.Controls.Add(_warehousePicker, 0, 0);
        mainLayout.Controls.Add(new Clovent.Desktop.Forms.Base.GridSpacer(), 0, 1);
        toolbar.Dock = DockStyle.Top;
        mainLayout.Controls.Add(toolbar, 0, 2);
        mainLayout.Controls.Add(new Clovent.Desktop.Forms.Base.GridSpacer(), 0, 3);
        _listView.Dock = DockStyle.Fill;
        mainLayout.Controls.Add(_listView, 0, 4);

        Controls.Add(mainLayout);
        Load += WarehouseStockManagementView_Load;
    }

    #endregion
}
