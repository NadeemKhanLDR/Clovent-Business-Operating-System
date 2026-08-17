using Clovent.Catalog.Application.Variants.Commands;
using Clovent.Catalog.Application.Variants.Dtos;
using Clovent.Desktop.MasterData;

namespace Clovent.Desktop.Catalog.Variants;

partial class ProductVariantManagementView
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

        _productPicker = new EntityPicker("Product:");

        _listView = new MasterDataListView<ProductVariantDto>(
        [
            new MasterDataColumn("Sku", "SKU", 120),
            new MasterDataColumn("Name", "Name", 220),
            new MasterDataColumn("Status", "Status", 90),
            new MasterDataColumn("CreatedAtUtc", "Created (UTC)", 160),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = dto => $"{dto.Sku} {dto.Name}",
            StatusSelector = dto => dto.Status,
            CanUseFeatureAsync = operation => CanUseFeatureAsync(operation),
            OnNew = CreateAsync,
            OnEdit = EditAsync,
            OnActivate = dto => _mediator.Send(new ActivateProductVariantCommand(dto.ProductVariantId)),
            OnDeactivate = dto => _mediator.Send(new DeactivateProductVariantCommand(dto.ProductVariantId)),
        };

        _productPicker.SelectionChanged += ProductPicker_SelectionChanged;

        Controls.Add(_listView);
        Controls.Add(_productPicker);
        Load += ProductVariantManagementView_Load;
    }

    #endregion

    private EntityPicker _productPicker;
    private MasterDataListView<ProductVariantDto> _listView;
}
