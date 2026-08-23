using Clovent.Desktop.MasterData;
using Clovent.Restaurant.Application.Tables.Commands;
using Clovent.Restaurant.Application.Tables.Dtos;

namespace Clovent.Desktop.Restaurant.Tables;

partial class TableManagementView
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    private readonly EntityPicker _diningAreaPicker = new("Dining Area:");
    private MasterDataListView<TableDto> _listView = null!;

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify the contents of
    /// this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;
        Name = "TableManagementView";

        _listView = new MasterDataListView<TableDto>(
        [
            new MasterDataColumn("Code", "Code", 90),
            new MasterDataColumn("Name", "Table Name", 120),
            new MasterDataColumn("Capacity", "Capacity", 80),
            new MasterDataColumn("OccupancyStatus", "Occupancy", 100),
            new MasterDataColumn("Status", "Status", 90),
            new MasterDataColumn("CreatedAtUtc", "Created (UTC)", 160),
        ],
        [
            new MasterDataListAction<TableDto>("Occupy", dto => _mediator.Send(new OccupyTableCommand(dto.TableId)),
                dto => dto.OccupancyStatus is "Available" or "Reserved", "occupy"),
            new MasterDataListAction<TableDto>("Vacate", dto => _mediator.Send(new VacateTableCommand(dto.TableId)),
                dto => dto.OccupancyStatus is "Occupied", "vacate"),
            new MasterDataListAction<TableDto>("Reserve", dto => _mediator.Send(new ReserveTableCommand(dto.TableId)),
                dto => dto.OccupancyStatus is "Available", "reserve"),
            new MasterDataListAction<TableDto>("Out of Service", dto => _mediator.Send(new SetTableOutOfServiceCommand(dto.TableId)),
                dto => dto.OccupancyStatus is "Available", "outofservice"),
            new MasterDataListAction<TableDto>("Return to Service", dto => _mediator.Send(new ReturnTableToServiceCommand(dto.TableId)),
                dto => dto.OccupancyStatus is "OutOfService", "returntoservice"),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = dto => $"{dto.Code} {dto.Name}",
            StatusSelector = dto => dto.Status,
            CanUseFeatureAsync = operation => CanUseFeatureAsync(operation),
            OnNew = CreateAsync,
            OnEdit = EditAsync,
            OnActivate = dto => _mediator.Send(new ActivateTableCommand(dto.TableId)),
            OnDeactivate = dto => _mediator.Send(new DeactivateTableCommand(dto.TableId)),
        };

        _diningAreaPicker.SelectionChanged += DiningAreaPicker_SelectionChanged;

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _diningAreaPicker.Dock = DockStyle.Top;
        mainLayout.Controls.Add(_diningAreaPicker, 0, 0);
        mainLayout.Controls.Add(new Clovent.Desktop.Forms.Base.GridSpacer(), 0, 1);
        _listView.Dock = DockStyle.Fill;
        mainLayout.Controls.Add(_listView, 0, 2);

        Controls.Add(mainLayout);
        Load += TableManagementView_Load;
    }

    #endregion
}
