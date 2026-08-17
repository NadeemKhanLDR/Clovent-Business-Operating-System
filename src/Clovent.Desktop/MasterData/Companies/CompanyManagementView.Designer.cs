using Clovent.Identity.Application.Companies.Commands;
using Clovent.Identity.Application.Companies.Dtos;

namespace Clovent.Desktop.MasterData.Companies;

partial class CompanyManagementView
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    private OrganizationHierarchySelector _selector;
    private MasterDataListView<CompanyDto> _listView;

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify the contents of
    /// this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;

        _listView = new MasterDataListView<CompanyDto>(
        [
            new MasterDataColumn("Name", "Name", 220),
            new MasterDataColumn("TaxId", "Tax Id", 140),
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
            OnActivate = dto => _mediator.Send(new ActivateCompanyCommand(dto.CompanyId)),
            OnDeactivate = dto => _mediator.Send(new DeactivateCompanyCommand(dto.CompanyId)),
        };

        _selector = new OrganizationHierarchySelector(_mediator, showCompany: false, showBranch: false);
        _selector.SelectionChanged += Selector_SelectionChanged;

        Controls.Add(_listView);
        Controls.Add(_selector);

        Load += CompanyManagementView_Load;
    }

    #endregion
}
