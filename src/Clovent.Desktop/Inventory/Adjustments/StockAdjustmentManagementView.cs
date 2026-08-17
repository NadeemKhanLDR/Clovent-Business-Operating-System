using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Inventory.Application.Adjustments.Commands;
using Clovent.Inventory.Application.Adjustments.Dtos;
using Clovent.Inventory.Application.Adjustments.Queries;
using Clovent.MasterData.Application.Warehouses.Queries;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Inventory.Adjustments;

/// <summary>
/// Stock Adjustment screen: search, filter, create, and Apply/Cancel over
/// the stock corrections proposed for a selected warehouse. Feature-gated
/// per <c>stockadjustments.{create|apply|cancel}</c>. Control tree (list
/// view, warehouse filter) lives in <c>StockAdjustmentManagementView.Designer.cs</c>;
/// this file holds behavior only.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed partial class StockAdjustmentManagementView : XtraUserControl
{
    private const string FeatureCode = "stockadjustments";

    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public StockAdjustmentManagementView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _featurePolicy = _scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>();
        _currentSession = currentSession;

        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _scope.Dispose();
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private async void WarehousePicker_SelectionChanged(object? sender, EventArgs e) => await _listView.RefreshAsync();

    private async void StockAdjustmentManagementView_Load(object? sender, EventArgs e) => await LoadWarehousesAsync();

    private async Task LoadWarehousesAsync()
    {
        var warehouses = await _mediator.Send(new ListAllWarehousesQuery());
        _warehousePicker.LoadItems([.. warehouses.Select(w => (w.WarehouseId, w.Name))]);
    }

    private async Task<IReadOnlyList<StockAdjustmentDto>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        if (_warehousePicker.SelectedId is not { } warehouseId)
        {
            return [];
        }

        var items = await _mediator.Send(new ListStockAdjustmentsByWarehouseQuery(warehouseId), cancellationToken);
        return [.. items];
    }

    private Task<bool> CanUseFeatureAsync(string operation) =>
        _currentSession.UserId is { } userId
            ? _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}")
            : Task.FromResult(false);

    private async Task CreateAsync()
    {
        if (_warehousePicker.SelectedId is not { } warehouseId)
        {
            XtraMessageBox.Show(this, "Select a warehouse first.", "No Warehouse Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var variants = await _mediator.Send(new ListProductVariantsQuery());
        var variantOptions = variants.Select(v => (v.ProductVariantId, $"{v.Sku} - {v.Name}")).ToList();
        if (variantOptions.Count == 0)
        {
            XtraMessageBox.Show(this, "Create a product variant first.", "No Variants Available", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var form = new StockAdjustmentCreateForm(variantOptions);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new CreateStockAdjustmentCommand(warehouseId, form.VariantId!.Value, form.AdjustmentType, form.Quantity, form.Reason));
        }
    }
}
