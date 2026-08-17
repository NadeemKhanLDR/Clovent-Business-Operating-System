using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.Restaurant.Orders;

partial class OrderHistoryView
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    private MasterDataListView<ClosedOrderRow> _listView = null!;

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify the contents of
    /// this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;
        Name = "OrderHistoryView";

        _listView = new MasterDataListView<ClosedOrderRow>(
        [
            new MasterDataColumn("OrderNumber", "Order #", 140),
            new MasterDataColumn("DailySalesNumber", "Daily #", 70),
            new MasterDataColumn("Status", "Status", 100),
            new MasterDataColumn("OrderType", "Type", 90),
            new MasterDataColumn("TableCode", "Table", 80),
            new MasterDataColumn("GrandTotal", "Grand Total", 110),
            new MasterDataColumn("PaidTotal", "Paid", 110),
            new MasterDataColumn("Balance", "Balance", 110),
            new MasterDataColumn("PaymentCount", "Payments", 80),
            new MasterDataColumn("CreatedAtUtc", "Opened (UTC)", 160),
            new MasterDataColumn("UpdatedAtUtc", "Closed (UTC)", 160),
        ],
        [
            // Gated on pos.void - the existing permission for reversing a
            // recorded sale, which is the only thing this screen can do. No new
            // permission code is introduced.
            new MasterDataListAction<ClosedOrderRow>("Payment History", ShowPaymentHistoryAsync, FeatureOperation: "void"),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = row => OrderHistoryRules.SearchText(row.OrderNumber, row.TableCode, row.Status, row.DailySalesNumber),
            CanUseFeatureAsync = operation => CanUseFeatureAsync(operation),
        };

        Controls.Add(_listView);
        Load += OrderHistoryView_Load;
    }

    #endregion
}
