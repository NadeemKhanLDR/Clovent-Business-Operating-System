using Clovent.Identity.Application.Branches.Commands;
using Clovent.Identity.Application.Branches.Dtos;

namespace Clovent.Desktop.MasterData.Branches;

partial class BranchManagementView
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    private OrganizationHierarchySelector _selector;
    private MasterDataListView<BranchDto> _listView;

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify the contents of
    /// this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        SuspendLayout();

        Dock = DockStyle.Fill;

        _listView = new MasterDataListView<BranchDto>(
        [
            new MasterDataColumn("Name", "Name", 200),
            new MasterDataColumn("City", "City", 120),
            new MasterDataColumn("Country", "Country", 120),
            new MasterDataColumn("Status", "Status", 90),
            new MasterDataColumn("CreatedAtUtc", "Created (UTC)", 160),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = dto => dto.Name,
            StatusSelector = dto => dto.Status,
            CanUseFeatureAsync = CanUseFeatureAsync,
            OnNew = CreateAsync,
            OnEdit = EditAsync,
            OnActivate = dto => _mediator.Send(new ActivateBranchCommand(dto.BranchId)),
            OnDeactivate = dto => _mediator.Send(new DeactivateBranchCommand(dto.BranchId)),
        };
        _listView.Name = "_listView";

        _selector = new OrganizationHierarchySelector(_mediator, showCompany: true, showBranch: false);
        _selector.Name = "_selector";
        _selector.SelectionChanged += Selector_SelectionChanged;

        //
        // BranchManagementView
        //
        Controls.Add(_listView);
        Controls.Add(_selector);
        Name = "BranchManagementView";
        Load += BranchManagementView_Load;

        ResumeLayout(false);
    }

    #endregion
}
