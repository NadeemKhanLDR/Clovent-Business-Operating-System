using Clovent.Catalog.Application.Products.Queries;
using Clovent.Catalog.Application.UnitsOfMeasure.Queries;
using Clovent.Catalog.Application.Variants.Commands;
using Clovent.Catalog.Application.Variants.Dtos;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Desktop.MasterData;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Catalog.Variants;

/// <summary>
/// Variant Management screen: search, filter, CRUD, activate/deactivate over
/// the variants of a selected product - the sellable units
/// <see cref="Clovent.Catalog.Barcodes.Barcode"/>, <see cref="Clovent.Catalog.Prices.ProductPrice"/>,
/// and warehouse stock all attach to. Feature-gated per
/// <c>variants.{create|edit|activate|deactivate}</c>.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed partial class ProductVariantManagementView : XtraUserControl
{
    private const string FeatureCode = "variants";

    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public ProductVariantManagementView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _featurePolicy = _scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>();
        _currentSession = currentSession;

        InitializeComponent();
    }

    private async void ProductPicker_SelectionChanged(object? sender, EventArgs e) => await _listView.RefreshAsync();

    private async void ProductVariantManagementView_Load(object? sender, EventArgs e) => await LoadProductsAsync();

    private async Task LoadProductsAsync()
    {
        var products = await _mediator.Send(new ListProductsQuery());
        _productPicker.LoadItems([.. products.Select(p => (p.ProductId, $"{p.Sku} - {p.Name}"))]);
    }

    private async Task<IReadOnlyList<ProductVariantDto>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        if (_productPicker.SelectedId is not { } productId)
        {
            return [];
        }

        var items = await _mediator.Send(new ListProductVariantsByProductQuery(productId), cancellationToken);
        return [.. items];
    }

    private Task<bool> CanUseFeatureAsync(string operation) =>
        _currentSession.UserId is { } userId
            ? _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}")
            : Task.FromResult(false);

    private async Task<IReadOnlyList<(Guid Id, string Display)>> LoadUnitOptionsAsync() =>
        [.. (await _mediator.Send(new ListUnitsOfMeasureQuery())).Select(u => (u.UnitOfMeasureId, $"{u.Code} - {u.Name}"))];

    private async Task CreateAsync()
    {
        if (_productPicker.SelectedId is not { } productId)
        {
            XtraMessageBox.Show(this, "Select a product first.", "No Product Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var units = await LoadUnitOptionsAsync();
        if (units.Count == 0)
        {
            XtraMessageBox.Show(this, "Create a unit of measure first.", "No Units Available", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var form = new ProductVariantEditForm("New Variant", units);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new CreateProductVariantCommand(productId, form.NameValue, form.SkuValue, form.UnitOfMeasureId!.Value));
        }
    }

    private async Task EditAsync(ProductVariantDto dto)
    {
        using var form = new ProductVariantEditForm("Edit Variant", await LoadUnitOptionsAsync(), dto.Name, dto.Sku, dto.UnitOfMeasureId, isNew: false);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new RenameProductVariantCommand(dto.ProductVariantId, form.NameValue));
            await _mediator.Send(new SetProductVariantUnitOfMeasureCommand(dto.ProductVariantId, form.UnitOfMeasureId!.Value));
        }
    }
}
