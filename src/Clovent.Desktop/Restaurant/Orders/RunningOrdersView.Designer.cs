using System.Drawing;
using System.Windows.Forms;
using Clovent.Desktop.MasterData;
using Clovent.Restaurant.Application.KitchenTickets.Commands;
using Clovent.Restaurant.Application.Orders.Commands;
using DevExpress.XtraEditors;

namespace Clovent.Desktop.Restaurant.Orders;

partial class RunningOrdersView
{
    private System.ComponentModel.IContainer components = null;

    private MasterDataListView<OrderRow> _listView = null!;
    private ComboBoxEdit _comboOrderTypeFilter = null!;

    #region Component Designer generated code

    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;
        Name = "RunningOrdersView";

        _comboOrderTypeFilter = new ComboBoxEdit();
        _comboOrderTypeFilter.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
        _comboOrderTypeFilter.Properties.Items.AddRange(new object[] { "All Orders", "DineIn", "TakeAway", "Delivery" });
        _comboOrderTypeFilter.SelectedIndex = 0;
        _comboOrderTypeFilter.Properties.Appearance.Font = new Font("Segoe UI", 9.5F);
        _comboOrderTypeFilter.Properties.Appearance.Options.UseFont = true;
        _comboOrderTypeFilter.Size = new Size(150, 26);
        _comboOrderTypeFilter.SelectedIndexChanged += ComboOrderTypeFilter_SelectedIndexChanged;

        var topFilterPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(12, 6, 12, 4),
            FlowDirection = FlowDirection.LeftToRight
        };

        var lblFilter = new LabelControl
        {
            Text = "Order Type:",
            AutoSizeMode = LabelAutoSizeMode.Horizontal,
            Padding = new Padding(0, 4, 8, 0)
        };
        lblFilter.Appearance.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        lblFilter.Appearance.Options.UseFont = true;

        topFilterPanel.Controls.Add(lblFilter);
        topFilterPanel.Controls.Add(_comboOrderTypeFilter);

        _listView = new MasterDataListView<OrderRow>(
        [
            new MasterDataColumn("OrderNumber", "Order #", 120),
            new MasterDataColumn("OrderType", "Type", 85),
            new MasterDataColumn("TableCode", "Table", 75),
            new MasterDataColumn("DeliveryStatus", "Delivery Status", 115),
            new MasterDataColumn("RiderName", "Rider", 95),
            new MasterDataColumn("CustomerInfo", "Customer", 130),
            new MasterDataColumn("LineCount", "Lines", 55),
            new MasterDataColumn("Notes", "Notes", 180),
            new MasterDataColumn("CreatedAtUtc", "Opened (UTC)", 135),
        ],
        [
            new MasterDataListAction<OrderRow>("Hold", row => _mediator.Send(new HoldOrderCommand(row.OrderId)), FeatureOperation: "hold"),
            new MasterDataListAction<OrderRow>("Send to Kitchen", row => _mediator.Send(new SendOrderToKitchenCommand(row.OrderId)), FeatureOperation: "sendtokitchen"),
            new MasterDataListAction<OrderRow>("Preparing", SetPreparingAsync, row => row.OrderType == "Delivery" && row.DeliveryStatus is "Received" or "None", FeatureOperation: "edit"),
            new MasterDataListAction<OrderRow>("Ready", SetReadyAsync, row => row.OrderType == "Delivery" && row.DeliveryStatus is "Preparing", FeatureOperation: "edit"),
            new MasterDataListAction<OrderRow>("Assign Rider", AssignRiderAsync, row => row.OrderType == "Delivery" && row.DeliveryStatus is "Ready" or "Preparing", FeatureOperation: "edit"),
            new MasterDataListAction<OrderRow>("Delivered", SetDeliveredAsync, row => row.OrderType == "Delivery" && row.DeliveryStatus is "OutForDelivery", FeatureOperation: "edit"),
            new MasterDataListAction<OrderRow>("Void", VoidAsync, FeatureOperation: "void"),
            new MasterDataListAction<OrderRow>("Cancel", CancelAsync, FeatureOperation: "cancel"),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = row => $"{row.OrderNumber} {row.TableCode} {row.CustomerInfo} {row.RiderName}",
            CanUseFeatureAsync = operation => CanUseFeatureAsync(operation),
        };

        Controls.Add(_listView);
        Controls.Add(topFilterPanel);
        Load += RunningOrdersView_Load;
    }

    #endregion
}
