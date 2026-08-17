using Clovent.Desktop.MasterData;
using Clovent.Restaurant.Application.DiningAreas.Commands;
using Clovent.Restaurant.Application.DiningAreas.Dtos;

namespace Clovent.Desktop.Restaurant.DiningAreas;

partial class DiningAreaManagementView
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    private OrganizationHierarchySelector _selector = null!;
    private MasterDataListView<DiningAreaDto> _listView = null!;

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify the contents of
    /// this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;
        Name = "DiningAreaManagementView";

        _listView = new MasterDataListView<DiningAreaDto>(
        [
            new MasterDataColumn("Name", "Name", 220),
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
            OnActivate = dto => _mediator.Send(new ActivateDiningAreaCommand(dto.DiningAreaId)),
            OnDeactivate = dto => _mediator.Send(new DeactivateDiningAreaCommand(dto.DiningAreaId)),
        };

        _selector = new OrganizationHierarchySelector(_mediator, showCompany: true, showBranch: true);
        _selector.SelectionChanged += Selector_SelectionChanged;

        Controls.Add(_listView);
        Controls.Add(_selector);
        Load += DiningAreaManagementView_Load;
    }

    #endregion
}
