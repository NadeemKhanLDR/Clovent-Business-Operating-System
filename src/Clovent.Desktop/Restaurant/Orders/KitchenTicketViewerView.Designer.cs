using Clovent.Desktop.MasterData;
using Clovent.Restaurant.Application.KitchenTickets.Commands;

namespace Clovent.Desktop.Restaurant.Orders;

partial class KitchenTicketViewerView
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    private MasterDataListView<KitchenTicketRow> _listView = null!;

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify the contents of
    /// this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;
        Name = "KitchenTicketViewerView";

        _listView = new MasterDataListView<KitchenTicketRow>(
        [
            new MasterDataColumn("OrderNumber", "Order #", 140),
            new MasterDataColumn("LineCount", "Lines", 60),
            new MasterDataColumn("Status", "Status", 90),
            new MasterDataColumn("CreatedAtUtc", "Sent (UTC)", 160),
            new MasterDataColumn("StartedAtUtc", "Started (UTC)", 160),
            new MasterDataColumn("ReadyAtUtc", "Ready (UTC)", 160),
        ],
        [
            new MasterDataListAction<KitchenTicketRow>("Start", row => _mediator.Send(new StartKitchenTicketCommand(row.KitchenTicketId)),
                row => row.Status == "New", "start"),
            new MasterDataListAction<KitchenTicketRow>("Mark Ready", row => _mediator.Send(new MarkKitchenTicketReadyCommand(row.KitchenTicketId)),
                row => row.Status == "InProgress", "markready"),
            new MasterDataListAction<KitchenTicketRow>("Serve", row => _mediator.Send(new ServeKitchenTicketCommand(row.KitchenTicketId)),
                row => row.Status == "Ready", "serve"),
            new MasterDataListAction<KitchenTicketRow>("Cancel", row => _mediator.Send(new CancelKitchenTicketCommand(row.KitchenTicketId)),
                row => row.Status is "New" or "InProgress", "cancel"),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = row => row.OrderNumber,
            CanUseFeatureAsync = operation => CanUseFeatureAsync(operation),
        };

        Controls.Add(_listView);
        Load += KitchenTicketViewerView_Load;
    }

    #endregion
}
