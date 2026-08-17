using Clovent.Catalog.Application.Barcodes.Commands;
using Clovent.Catalog.Application.Barcodes.Dtos;
using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.Catalog.Barcodes;

partial class BarcodeManagementView
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>Clean up any resources being used.</summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _scope.Dispose();
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Component Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify the contents of
    /// this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;

        _variantPicker = new EntityPicker("Variant:", comboWidth: 320);

        _listView = new MasterDataListView<BarcodeDto>(
        [
            new MasterDataColumn("Value", "Value", 140),
            new MasterDataColumn("IsPrimary", "Primary", 70),
            new MasterDataColumn("Status", "Status", 90),
            new MasterDataColumn("CreatedAtUtc", "Created (UTC)", 160),
        ],
        [
            new MasterDataListAction<BarcodeDto>("Mark Primary", dto => _mediator.Send(new MarkBarcodeAsPrimaryCommand(dto.BarcodeId)), dto => !dto.IsPrimary, "markprimary"),
            new MasterDataListAction<BarcodeDto>("Unmark Primary", dto => _mediator.Send(new UnmarkBarcodeAsPrimaryCommand(dto.BarcodeId)), dto => dto.IsPrimary, "markprimary"),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = dto => dto.Value,
            StatusSelector = dto => dto.Status,
            CanUseFeatureAsync = operation => CanUseFeatureAsync(operation),
            OnNew = CreateAsync,
            OnActivate = dto => _mediator.Send(new ActivateBarcodeCommand(dto.BarcodeId)),
            OnDeactivate = dto => _mediator.Send(new DeactivateBarcodeCommand(dto.BarcodeId)),
        };

        _variantPicker.SelectionChanged += VariantPicker_SelectionChanged;

        Controls.Add(_listView);
        Controls.Add(_variantPicker);
        Load += BarcodeManagementView_Load;
    }

    #endregion

    private EntityPicker _variantPicker;
    private MasterDataListView<BarcodeDto> _listView;
}
