using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.Inventory.Transactions;

partial class InventoryTransactionsView
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    private EntityPicker _warehousePicker;
    private EntityPicker _productPicker;
    private MasterDataListView<InventoryTransactionRow> _listView;

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify the contents of
    /// this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;

        _warehousePicker = new EntityPicker("Warehouse:");
        _productPicker = new EntityPicker("Product (Stock History):");

        _listView = new MasterDataListView<InventoryTransactionRow>(
        [
            new MasterDataColumn("Sku", "SKU", 100),
            new MasterDataColumn("ProductName", "Product", 160),
            new MasterDataColumn("TransactionType", "Type", 100),
            new MasterDataColumn("Quantity", "Quantity", 90),
            new MasterDataColumn("ReferenceType", "Reference", 120),
            new MasterDataColumn("Notes", "Notes", 200),
            new MasterDataColumn("OccurredAtUtc", "Occurred (UTC)", 160),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = row => $"{row.Sku} {row.ProductName} {row.TransactionType} {row.ReferenceType} {row.Notes}",
            CanUseFeatureAsync = operation => CanUseFeatureAsync(operation),
        };

        // Product takes precedence when both are selected - see LoadItemsAsync.
        _warehousePicker.SelectionChanged += WarehousePicker_SelectionChanged;
        _productPicker.SelectionChanged += ProductPicker_SelectionChanged;

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _warehousePicker.Dock = DockStyle.Top;
        mainLayout.Controls.Add(_warehousePicker, 0, 0);
        _productPicker.Dock = DockStyle.Top;
        mainLayout.Controls.Add(_productPicker, 0, 1);
        mainLayout.Controls.Add(new Clovent.Desktop.Forms.Base.GridSpacer(), 0, 2);
        _listView.Dock = DockStyle.Fill;
        mainLayout.Controls.Add(_listView, 0, 3);

        Controls.Add(mainLayout);
        Load += InventoryTransactionsView_Load;
    }

    #endregion
}
