using Clovent.MasterData.Application.Terminals.Commands;
using Clovent.MasterData.Application.Terminals.Dtos;

namespace Clovent.Desktop.MasterData.Terminals;

partial class TerminalManagementView
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    private OrganizationHierarchySelector _selector;
    private MasterDataListView<TerminalDto> _listView;

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify the contents of
    /// this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;

        _listView = new MasterDataListView<TerminalDto>(
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
            OnActivate = dto => _mediator.Send(new ActivateTerminalCommand(dto.TerminalId)),
            OnDeactivate = dto => _mediator.Send(new DeactivateTerminalCommand(dto.TerminalId)),
        };

        _selector = new OrganizationHierarchySelector(_mediator, showCompany: true, showBranch: true);
        _selector.SelectionChanged += Selector_SelectionChanged;

        Controls.Add(_listView);
        Controls.Add(_selector);

        Load += TerminalManagementView_Load;
    }

    #endregion
}
