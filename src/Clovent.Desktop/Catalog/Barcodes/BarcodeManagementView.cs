using Clovent.Catalog.Application.Barcodes.Commands;
using Clovent.Catalog.Application.Barcodes.Dtos;
using Clovent.Catalog.Application.Barcodes.Queries;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Desktop.MasterData;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Catalog.Barcodes;

/// <summary>
/// Barcode Management screen: search, filter, create, activate/deactivate,
/// and Mark/Unmark Primary over the barcodes of a selected variant.
/// Feature-gated per <c>barcodes.{create|activate|deactivate|markprimary}</c>.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed partial class BarcodeManagementView : XtraUserControl
{
    private const string FeatureCode = "barcodes";

    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public BarcodeManagementView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _featurePolicy = _scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>();
        _currentSession = currentSession;

        InitializeComponent();
    }

    private async void VariantPicker_SelectionChanged(object? sender, EventArgs e) => await _listView.RefreshAsync();

    private async void BarcodeManagementView_Load(object? sender, EventArgs e) => await LoadVariantsAsync();

    private async Task LoadVariantsAsync()
    {
        var variants = await _mediator.Send(new ListProductVariantsQuery());
        _variantPicker.LoadItems([.. variants.Select(v => (v.ProductVariantId, $"{v.Sku} - {v.Name}"))]);
    }

    private async Task<IReadOnlyList<BarcodeDto>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        if (_variantPicker.SelectedId is not { } variantId)
        {
            return [];
        }

        var items = await _mediator.Send(new ListBarcodesByVariantQuery(variantId), cancellationToken);
        return [.. items];
    }

    private Task<bool> CanUseFeatureAsync(string operation) =>
        _currentSession.UserId is { } userId
            ? _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}")
            : Task.FromResult(false);

    private async Task CreateAsync()
    {
        if (_variantPicker.SelectedId is not { } variantId)
        {
            XtraMessageBox.Show(this, "Select a variant first.", "No Variant Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var form = new BarcodeCreateForm();
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new CreateBarcodeCommand(variantId, form.Value, form.IsPrimary));
        }
    }
}
