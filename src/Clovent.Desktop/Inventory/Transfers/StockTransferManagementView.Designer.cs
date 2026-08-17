using Clovent.Desktop.MasterData;
using Clovent.Inventory.Application.Transfers.Commands;

namespace Clovent.Desktop.Inventory.Transfers;

partial class StockTransferManagementView
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    private MasterDataListView<StockTransferRow> _listView;

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify the contents of
    /// this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;

        _listView = new MasterDataListView<StockTransferRow>(
        [
            new MasterDataColumn("Sku", "SKU", 100),
            new MasterDataColumn("ProductName", "Product", 160),
            new MasterDataColumn("SourceWarehouseName", "From", 130),
            new MasterDataColumn("DestinationWarehouseName", "To", 130),
            new MasterDataColumn("Quantity", "Quantity", 90),
            new MasterDataColumn("Status", "Status", 90),
            new MasterDataColumn("CreatedAtUtc", "Created (UTC)", 160),
            new MasterDataColumn("CompletedAtUtc", "Completed (UTC)", 160),
        ],
        [
            new MasterDataListAction<StockTransferRow>("Complete", row => _mediator.Send(new CompleteStockTransferCommand(row.Source.StockTransferId)), row => row.Status == "Pending", "complete"),
            new MasterDataListAction<StockTransferRow>("Cancel", row => _mediator.Send(new CancelStockTransferCommand(row.Source.StockTransferId)), row => row.Status == "Pending", "cancel"),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = row => $"{row.Sku} {row.ProductName} {row.SourceWarehouseName} {row.DestinationWarehouseName}",
            CanUseFeatureAsync = operation => CanUseFeatureAsync(operation),
            OnNew = CreateAsync,
        };

        Controls.Add(_listView);
        Load += StockTransferManagementView_Load;
    }

    #endregion
}
