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

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _variantPicker.Dock = DockStyle.Top;
        mainLayout.Controls.Add(_variantPicker, 0, 0);
        mainLayout.Controls.Add(new Clovent.Desktop.Forms.Base.GridSpacer(), 0, 1);
        _listView.Dock = DockStyle.Fill;
        mainLayout.Controls.Add(_listView, 0, 2);

        Controls.Add(mainLayout);
        Load += ProductPriceManagementView_Load;
    }

    #endregion

    private EntityPicker _variantPicker;
    private MasterDataListView<ProductPriceDto> _listView;
}
