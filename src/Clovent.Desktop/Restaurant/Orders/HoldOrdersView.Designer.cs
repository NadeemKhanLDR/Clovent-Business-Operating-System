using Clovent.Desktop.MasterData;
using Clovent.Restaurant.Application.Orders.Commands;

namespace Clovent.Desktop.Restaurant.Orders;

partial class HoldOrdersView
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    private MasterDataListView<HeldOrderRow> _listView = null!;

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify the contents of
    /// this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;
        Name = "HoldOrdersView";

        _listView = new MasterDataListView<HeldOrderRow>(
        [
            new MasterDataColumn("OrderNumber", "Order #", 140),
            new MasterDataColumn("OrderType", "Type", 90),
            new MasterDataColumn("TableCode", "Table", 90),
            new MasterDataColumn("LineCount", "Lines", 60),
            new MasterDataColumn("Notes", "Notes", 200),
            new MasterDataColumn("UpdatedAtUtc", "Held Since (UTC)", 160),
        ],
        [
            new MasterDataListAction<HeldOrderRow>("Resume", row => _mediator.Send(new ResumeOrderCommand(row.OrderId)), FeatureOperation: "resume"),
            new MasterDataListAction<HeldOrderRow>("Cancel", CancelAsync, FeatureOperation: "cancel"),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = row => $"{row.OrderNumber} {row.TableCode}",
            CanUseFeatureAsync = operation => CanUseFeatureAsync(operation),
        };

        Controls.Add(_listView);
        Load += HoldOrdersView_Load;
    }

    #endregion
}
