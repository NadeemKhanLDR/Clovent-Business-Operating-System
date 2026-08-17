using Clovent.Catalog.Application.Prices.Commands;
using Clovent.Catalog.Application.Prices.Dtos;
using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.Catalog.Prices;

partial class ProductPriceManagementView
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

        _listView = new MasterDataListView<ProductPriceDto>(
        [
            new MasterDataColumn("PriceType", "Type", 80),
            new MasterDataColumn("Amount", "Amount", 100),
            new MasterDataColumn("EffectiveFromUtc", "Effective From (UTC)", 160),
            new MasterDataColumn("Status", "Status", 90),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = dto => dto.PriceType,
            StatusSelector = dto => dto.Status,
            CanUseFeatureAsync = operation => CanUseFeatureAsync(operation),
            OnNew = CreateAsync,
            OnEdit = EditAsync,
            OnActivate = dto => _mediator.Send(new ActivateProductPriceCommand(dto.ProductPriceId)),
            OnDeactivate = dto => _mediator.Send(new DeactivateProductPriceCommand(dto.ProductPriceId)),
        };

        _variantPicker.SelectionChanged += VariantPicker_SelectionChanged;

        Controls.Add(_listView);
        Controls.Add(_variantPicker);
        Load += ProductPriceManagementView_Load;
    }

    #endregion

    private EntityPicker _variantPicker;
    private MasterDataListView<ProductPriceDto> _listView;
}
