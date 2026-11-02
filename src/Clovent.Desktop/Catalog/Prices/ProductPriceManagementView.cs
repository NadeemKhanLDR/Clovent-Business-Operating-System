using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Catalog.Application.Prices.Commands;
using Clovent.Catalog.Application.Prices.Dtos;
using Clovent.Catalog.Application.Prices.Queries;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Desktop.Forms.Base;
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
    private readonly ScreenOperationGate _gate = new();
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;
    private readonly CancellationTokenSource _lifecycleCts = new();
    private bool _isLoadingVariants;

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public ProductPriceManagementView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = new SerializedMediator(_scope.ServiceProvider.GetRequiredService<IMediator>(), _gate);
        _featurePolicy = new SerializedFeatureAuthorizationPolicy(_scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>(), _gate);
        _currentSession = currentSession;

        InitializeComponent();
        _listView.GridView.CustomColumnDisplayText += GridView_CustomColumnDisplayText;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                _lifecycleCts.Cancel();
            }
            catch (ObjectDisposedException) { }

            _gate.Drain(TimeSpan.FromMilliseconds(500));
            _scope.Dispose();
            _gate.Dispose();
            try
            {
                _lifecycleCts.Dispose();
            }
            catch (ObjectDisposedException) { }

            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void GridView_CustomColumnDisplayText(object? sender, DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventArgs e)
    {
        if (e.Column.FieldName == "Amount" && e.Value != null && e.Value != DBNull.Value)
        {
            try
            {
                var val = Convert.ToDecimal(e.Value);
                e.DisplayText = CurrencyDisplay.FormatPlain(val);
            }
            catch
            {
                // Fallback
            }
        }
    }

    private async void VariantPicker_SelectionChanged(object? sender, EventArgs e)
    {
        if (_isLoadingVariants || _lifecycleCts.IsCancellationRequested || IsDisposed || Disposing)
        {
            return;
        }

        await _listView.RefreshAsync(_lifecycleCts.Token);
    }

    private async void ProductPriceManagementView_Load(object? sender, EventArgs e) => await LoadVariantsAsync();

    private async Task LoadVariantsAsync()
    {
        _isLoadingVariants = true;
        try
        {
            var variants = await _mediator.Send(new ListProductVariantsQuery(), _lifecycleCts.Token);
            _variantPicker.LoadItems([.. variants.Select(v => (v.ProductVariantId, $"{v.Sku} - {v.Name}"))]);
        }
        catch (OperationCanceledException) when (_lifecycleCts.IsCancellationRequested)
        {
            return;
        }
        finally
        {
            _isLoadingVariants = false;
        }

        if (!_lifecycleCts.IsCancellationRequested && !IsDisposed && !Disposing)
        {
            await _listView.RefreshAsync(_lifecycleCts.Token);
        }
    }

    private async Task<IReadOnlyList<ProductPriceDto>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        if (_variantPicker.SelectedId is not { } variantId)
        {
            return [];
        }

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifecycleCts.Token);
        var items = await _mediator.Send(new ListProductPricesByVariantQuery(variantId), linked.Token);
        return [.. items];
    }

    private Task<bool> CanUseFeatureAsync(string operation) =>
        _currentSession.UserId is { } userId && !_lifecycleCts.IsCancellationRequested && !IsDisposed && !Disposing
            ? _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}", _lifecycleCts.Token)
            : Task.FromResult(false);

    private async Task<IReadOnlyList<(Guid Id, string Display)>> LoadCurrencyOptionsAsync()
    {
        var currencies = await _mediator.Send(new ListCurrenciesQuery(), _lifecycleCts.Token);
        return [.. currencies.Select(c => (c.CurrencyId, $"{c.Code} - {c.Name}"))];
    }

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
            await _mediator.Send(new CreateProductPriceCommand(variantId, form.PriceType, form.Amount, form.CurrencyId!.Value), _lifecycleCts.Token);
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
            await _mediator.Send(new UpdateProductPriceAmountCommand(dto.ProductPriceId, form.Amount), _lifecycleCts.Token);
        }
    }
}
