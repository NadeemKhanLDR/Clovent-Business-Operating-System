using System.Windows.Forms;
using Clovent.Catalog.Application.TaxProfiles.Commands;
using Clovent.Catalog.Application.TaxProfiles.Dtos;
using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.Forms.Settings;

partial class TaxProfileManagementView
{
    private System.ComponentModel.IContainer components = null;
    private MasterDataListView<TaxProfileDto> _listView = null!;

    #region Component Designer generated code

    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;

        _listView = new MasterDataListView<TaxProfileDto>(
        [
            new MasterDataColumn("Code", "Tax Code", 110),
            new MasterDataColumn("InvoiceDisplayName", "Invoice Display Name", 180),
            new MasterDataColumn("Authority", "Authority", 80),
            new MasterDataColumn("Jurisdiction", "Jurisdiction", 90),
            new MasterDataColumn("TaxClassification", "Taxability", 90),
            new MasterDataColumn("ItemClassification", "Item Type", 90),
            new MasterDataColumn("RatePercentage", "Rate %", 70),
            new MasterDataColumn("PricingMode", "Pricing Mode", 90),
            new MasterDataColumn("IsActive", "Active", 70),
            new MasterDataColumn("ReferenceDocument", "Statutory Ref", 140)
        ],
        [
            new MasterDataListAction<TaxProfileDto>("Edit Profile", EditSelectedProfileAsync)
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = dto => $"{dto.Code} {dto.InvoiceDisplayName} {dto.Authority} {dto.Jurisdiction}",
            StatusSelector = dto => dto.IsActive ? "Active" : "Inactive",
            CanUseFeatureAsync = operation => CanUseFeatureAsync(operation),
            OnNew = CreateAsync,
            OnActivate = dto => _mediator?.Send(new ToggleTaxProfileStatusCommand(dto.TaxProfileId, true)) ?? System.Threading.Tasks.Task.CompletedTask,
            OnDeactivate = dto => _mediator?.Send(new ToggleTaxProfileStatusCommand(dto.TaxProfileId, false)) ?? System.Threading.Tasks.Task.CompletedTask,
        };

        Controls.Add(_listView);
        Load += TaxProfileManagementView_Load;
    }

    #endregion
}
