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

        Controls.Add(_listView);
        Controls.Add(_productPicker);
        Controls.Add(_warehousePicker);
        Load += InventoryTransactionsView_Load;
    }

    #endregion
}
