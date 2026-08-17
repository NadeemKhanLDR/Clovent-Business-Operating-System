using Clovent.MasterData.Application.Currencies.Commands;
using Clovent.MasterData.Application.Currencies.Dtos;

namespace Clovent.Desktop.MasterData.Currencies;

partial class CurrencyManagementView
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    private MasterDataListView<CurrencyDto> _listView;

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify the contents of
    /// this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;

        _listView = new MasterDataListView<CurrencyDto>(
        [
            new MasterDataColumn("Code", "Code", 80),
            new MasterDataColumn("Name", "Name", 180),
            new MasterDataColumn("Symbol", "Symbol", 70),
            new MasterDataColumn("DecimalPlaces", "Decimals", 80),
            new MasterDataColumn("Status", "Status", 90),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = dto => $"{dto.Code} {dto.Name}",
            StatusSelector = dto => dto.Status,
            CanUseFeatureAsync = operation => CanUseFeatureAsync(operation),
            OnNew = CreateAsync,
            OnActivate = dto => _mediator.Send(new ActivateCurrencyCommand(dto.CurrencyId)),
            OnDeactivate = dto => _mediator.Send(new DeactivateCurrencyCommand(dto.CurrencyId)),
        };

        Controls.Add(_listView);

        Load += CurrencyManagementView_Load;
    }

    #endregion
}
