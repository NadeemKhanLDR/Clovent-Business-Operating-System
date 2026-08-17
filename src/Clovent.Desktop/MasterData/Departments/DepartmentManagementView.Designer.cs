using Clovent.MasterData.Application.Departments.Commands;
using Clovent.MasterData.Application.Departments.Dtos;

namespace Clovent.Desktop.MasterData.Departments;

partial class DepartmentManagementView
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    private OrganizationHierarchySelector _selector;
    private MasterDataListView<DepartmentDto> _listView;

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify the contents of
    /// this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;

        _listView = new MasterDataListView<DepartmentDto>(
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
            OnActivate = dto => _mediator.Send(new ActivateDepartmentCommand(dto.DepartmentId)),
            OnDeactivate = dto => _mediator.Send(new DeactivateDepartmentCommand(dto.DepartmentId)),
        };

        _selector = new OrganizationHierarchySelector(_mediator, showCompany: true, showBranch: true);
        _selector.SelectionChanged += Selector_SelectionChanged;

        Controls.Add(_listView);
        Controls.Add(_selector);

        Load += DepartmentManagementView_Load;
    }

    #endregion
}
