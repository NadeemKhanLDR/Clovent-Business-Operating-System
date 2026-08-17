using Clovent.MasterData.Application.FiscalYears.Commands;
using Clovent.MasterData.Application.FiscalYears.Dtos;

namespace Clovent.Desktop.MasterData.FiscalYears;

partial class FiscalYearManagementView
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    private OrganizationHierarchySelector _selector;
    private MasterDataListView<FiscalYearDto> _listView;

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify the contents of
    /// this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;

        _listView = new MasterDataListView<FiscalYearDto>(
        [
            new MasterDataColumn("Name", "Name", 160),
            new MasterDataColumn("StartDate", "Start Date", 110),
            new MasterDataColumn("EndDate", "End Date", 110),
            new MasterDataColumn("Status", "Status", 90),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = dto => dto.Name,
            StatusSelector = dto => dto.Status == "Open" ? "Active" : "Inactive",
            DeactivateButtonText = "Close",
            CanUseFeatureAsync = operation => CanUseFeatureAsync(operation),
            OnNew = CreateAsync,
            OnEdit = EditAsync,
            OnDeactivate = dto => _mediator.Send(new CloseFiscalYearCommand(dto.FiscalYearId)),
        };

        _selector = new OrganizationHierarchySelector(_mediator, showCompany: false, showBranch: false);
        _selector.SelectionChanged += Selector_SelectionChanged;

        Controls.Add(_listView);
        Controls.Add(_selector);

        Load += FiscalYearManagementView_Load;
    }

    #endregion
}
