using Clovent.MasterData.Application.Warehouses.Commands;
using Clovent.MasterData.Application.Warehouses.Dtos;

namespace Clovent.Desktop.MasterData.Warehouses;

partial class WarehouseManagementView
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    private OrganizationHierarchySelector _selector;
    private MasterDataListView<WarehouseDto> _listView;

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify the contents of
    /// this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;

        _listView = new MasterDataListView<WarehouseDto>(
        [
            new MasterDataColumn("Name", "Name", 200),
            new MasterDataColumn("Code", "Code", 100),
            new MasterDataColumn("Status", "Status", 90),
            new MasterDataColumn("CreatedAtUtc", "Created (UTC)", 160),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = dto => dto.Name,
            StatusSelector = dto => dto.Status,
            CanUseFeatureAsync = operation => CanUseFeatureAsync(operation),
            OnNew = CreateAsync,
            OnEdit = EditAsync,
            OnActivate = dto => _mediator.Send(new ActivateWarehouseCommand(dto.WarehouseId)),
            OnDeactivate = dto => _mediator.Send(new DeactivateWarehouseCommand(dto.WarehouseId)),
        };

        _selector = new OrganizationHierarchySelector(_mediator, showCompany: true, showBranch: true);
        _selector.SelectionChanged += Selector_SelectionChanged;

        Controls.Add(_listView);
        Controls.Add(_selector);

        Load += WarehouseManagementView_Load;
    }

    #endregion
}
