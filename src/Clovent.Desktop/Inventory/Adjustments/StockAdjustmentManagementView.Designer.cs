using Clovent.Desktop.MasterData;
using Clovent.Inventory.Application.Adjustments.Commands;
using Clovent.Inventory.Application.Adjustments.Dtos;

namespace Clovent.Desktop.Inventory.Adjustments;

partial class StockAdjustmentManagementView
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    private EntityPicker _warehousePicker;
    private MasterDataListView<StockAdjustmentDto> _listView;

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify the contents of
    /// this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;

        _warehousePicker = new EntityPicker("Warehouse:");

        _listView = new MasterDataListView<StockAdjustmentDto>(
        [
            new MasterDataColumn("AdjustmentType", "Type", 80),
            new MasterDataColumn("Quantity", "Quantity", 90),
            new MasterDataColumn("Reason", "Reason", 220),
            new MasterDataColumn("Status", "Status", 90),
            new MasterDataColumn("CreatedAtUtc", "Created (UTC)", 160),
        ],
        [
            new MasterDataListAction<StockAdjustmentDto>("Apply", dto => _mediator.Send(new ApplyStockAdjustmentCommand(dto.StockAdjustmentId)), dto => dto.Status == "Pending", "apply"),
            new MasterDataListAction<StockAdjustmentDto>("Cancel", dto => _mediator.Send(new CancelStockAdjustmentCommand(dto.StockAdjustmentId)), dto => dto.Status == "Pending", "cancel"),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = dto => dto.Reason,
            CanUseFeatureAsync = operation => CanUseFeatureAsync(operation),
            OnNew = CreateAsync,
        };

        _warehousePicker.SelectionChanged += WarehousePicker_SelectionChanged;

        Controls.Add(_listView);
        Controls.Add(_warehousePicker);
        Load += StockAdjustmentManagementView_Load;
    }

    #endregion
}
