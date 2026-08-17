using Clovent.Catalog.Application.Prices.Commands;
using Clovent.Catalog.Application.Prices.Dtos;
using Clovent.Catalog.Application.Prices.Queries;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Desktop.MasterData;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.MasterData.Application.Currencies.Queries;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Catalog.Prices;

/// <summary>
/// Price Management screen: search, filter, CRUD, activate/deactivate over
/// the cost/selling prices of a selected variant - "Multiple prices" is a
/// natural consequence of <see cref="Clovent.Catalog.Prices.ProductPrice"/>
/// being its own aggregate rather than a single field (see
/// <c>CatalogArchitecture.md</c>). Feature-gated per
/// <c>prices.{create|edit|activate|deactivate}</c>.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed partial class ProductPriceManagementView : XtraUserControl
{
    private const string FeatureCode = "prices";

    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public ProductPriceManagementView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _featurePolicy = _scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>();
        _currentSession = currentSession;

        InitializeComponent();
    }

    private async void VariantPicker_SelectionChanged(object? sender, EventArgs e) => await _listView.RefreshAsync();

    private async void ProductPriceManagementView_Load(object? sender, EventArgs e) => await LoadVariantsAsync();

    private async Task LoadVariantsAsync()
    {
        var variants = await _mediator.Send(new ListProductVariantsQuery());
        _variantPicker.LoadItems([.. variants.Select(v => (v.ProductVariantId, $"{v.Sku} - {v.Name}"))]);
    }

    private async Task<IReadOnlyList<ProductPriceDto>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        if (_variantPicker.SelectedId is not { } variantId)
        {
            return [];
        }

        var items = await _mediator.Send(new ListProductPricesByVariantQuery(variantId), cancellationToken);
        return [.. items];
    }

    private Task<bool> CanUseFeatureAsync(string operation) =>
        _currentSession.UserId is { } userId
            ? _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}")
            : Task.FromResult(false);

    private async Task<IReadOnlyList<(Guid Id, string Display)>> LoadCurrencyOptionsAsync() =>
        [.. (await _mediator.Send(new ListCurrenciesQuery())).Select(c => (c.CurrencyId, $"{c.Code} - {c.Name}"))];

    private async Task CreateAsync()
    {
        if (_variantPicker.SelectedId is not { } variantId)
        {
            XtraMessageBox.Show(this, "Select a variant first.", "No Variant Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var currencies = await LoadCurrencyOptionsAsync();
        if (currencies.Count == 0)
        {
            XtraMessageBox.Show(this, "Create a currency first.", "No Currencies Available", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var form = new ProductPriceEditForm("New Price", currencies);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new CreateProductPriceCommand(variantId, form.PriceType, form.Amount, form.CurrencyId!.Value));
        }
    }

    private async Task EditAsync(ProductPriceDto dto)
    {
        using var form = new ProductPriceEditForm(
            "Edit Price",
            await LoadCurrencyOptionsAsync(),
            Enum.Parse<Clovent.Catalog.Prices.PriceType>(dto.PriceType),
            dto.CurrencyId,
            dto.Amount,
            isNew: false);

        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new UpdateProductPriceAmountCommand(dto.ProductPriceId, form.Amount));
        }
    }
}
