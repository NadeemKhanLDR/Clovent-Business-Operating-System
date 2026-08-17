using Clovent.Identity.Application.Organizations.Commands;
using Clovent.Identity.Application.Organizations.Dtos;

namespace Clovent.Desktop.MasterData.Organizations;

partial class OrganizationManagementView
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    private MasterDataListView<OrganizationDto> _listView;

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify the contents of
    /// this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;

        _listView = new MasterDataListView<OrganizationDto>(
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
            OnActivate = dto => _mediator.Send(new ActivateOrganizationCommand(dto.OrganizationId)),
            OnDeactivate = dto => _mediator.Send(new DeactivateOrganizationCommand(dto.OrganizationId)),
        };

        Controls.Add(_listView);

        Load += OrganizationManagementView_Load;
    }

    #endregion
}
