using Clovent.Desktop.MasterData;
using Clovent.Restaurant.Application.KitchenTickets.Commands;
using Clovent.Restaurant.Application.Orders.Commands;

namespace Clovent.Desktop.Restaurant.Orders;

partial class RunningOrdersView
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    private MasterDataListView<OrderRow> _listView = null!;

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify the contents of
    /// this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;
        Name = "RunningOrdersView";

        _listView = new MasterDataListView<OrderRow>(
        [
            new MasterDataColumn("OrderNumber", "Order #", 140),
            new MasterDataColumn("OrderType", "Type", 90),
            new MasterDataColumn("TableCode", "Table", 90),
            new MasterDataColumn("LineCount", "Lines", 60),
            new MasterDataColumn("Notes", "Notes", 200),
            new MasterDataColumn("CreatedAtUtc", "Opened (UTC)", 160),
        ],
        [
            new MasterDataListAction<OrderRow>("Hold", row => _mediator.Send(new HoldOrderCommand(row.OrderId)), FeatureOperation: "hold"),
            new MasterDataListAction<OrderRow>("Send to Kitchen", row => _mediator.Send(new SendOrderToKitchenCommand(row.OrderId)), FeatureOperation: "sendtokitchen"),
            new MasterDataListAction<OrderRow>("Void", VoidAsync, FeatureOperation: "void"),
            new MasterDataListAction<OrderRow>("Cancel", CancelAsync, FeatureOperation: "cancel"),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = row => $"{row.OrderNumber} {row.TableCode}",
            CanUseFeatureAsync = operation => CanUseFeatureAsync(operation),
        };

        Controls.Add(_listView);
        Load += RunningOrdersView_Load;
    }

    #endregion
}
