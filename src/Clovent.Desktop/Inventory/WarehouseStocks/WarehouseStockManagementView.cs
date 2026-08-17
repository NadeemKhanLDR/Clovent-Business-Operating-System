using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Inventory.Application.WarehouseStocks.Commands;
using Clovent.Inventory.Application.WarehouseStocks.Dtos;
using Clovent.Inventory.Application.WarehouseStocks.Queries;
using Clovent.MasterData.Application.Warehouses.Queries;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Inventory.WarehouseStocks;

/// <summary>
/// Warehouse Stock screen: search, filter, create, edit (stock levels and
/// negative-stock policy), Receive Inventory (opening stock/atomic
/// find-or-create receive), and Receive/Issue/Reserve/Release over the stock
/// balances of a selected warehouse. Feature-gated per
/// <c>warehousestocks.{create|edit|receive|issue|reserve|release}</c>.
/// Control tree (list view, warehouse filter, Receive Inventory toolbar)
/// lives in <c>WarehouseStockManagementView.Designer.cs</c>; this file holds
/// behavior only.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed partial class WarehouseStockManagementView : XtraUserControl
{
    private const string FeatureCode = "warehousestocks";

    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;

    private IReadOnlyList<(Guid Id, string Display)> _warehouseOptions = [];
    private Dictionary<Guid, (string Sku, string Name)> _variantsById = [];

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public WarehouseStockManagementView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
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

    private async void ReceiveInventoryButton_Click(object? sender, EventArgs e) => await ReceiveInventoryAsync();

    private async void WarehouseStockManagementView_Load(object? sender, EventArgs e) => await LoadLookupsAsync();

    private async Task LoadLookupsAsync()
    {
        var warehouses = await _mediator.Send(new ListAllWarehousesQuery());
        _warehouseOptions = [.. warehouses.Select(w => (w.WarehouseId, w.Name))];
        _warehousePicker.LoadItems(_warehouseOptions);

        var variants = await _mediator.Send(new ListProductVariantsQuery());
        _variantsById = variants.ToDictionary(v => v.ProductVariantId, v => (v.Sku, v.Name));
    }

    private async Task<IReadOnlyList<WarehouseStockRow>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        if (_warehousePicker.SelectedId is not { } warehouseId)
        {
            return [];
        }

        var items = await _mediator.Send(new ListWarehouseStocksByWarehouseQuery(warehouseId), cancellationToken);
        return
        [
            .. items.Select(dto =>
            {
                var (sku, name) = _variantsById.TryGetValue(dto.ProductVariantId, out var variant) ? variant : (string.Empty, string.Empty);
                return new WarehouseStockRow(sku, name, dto.QuantityOnHand, dto.QuantityReserved, dto.QuantityAvailable,
                    dto.MinimumStock, dto.MaximumStock, dto.AllowNegativeStock, dto.UpdatedAtUtc, dto);
            }),
        ];
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

        var variantOptions = _variantsById.Select(kv => (kv.Key, $"{kv.Value.Sku} - {kv.Value.Name}")).ToList();
        if (variantOptions.Count == 0)
        {
            XtraMessageBox.Show(this, "Create a product variant first.", "No Variants Available", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var form = new WarehouseStockEditForm("New Warehouse Stock", variantOptions);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new CreateWarehouseStockCommand(warehouseId, form.VariantId!.Value, form.MinimumStock, form.MaximumStock, form.AllowNegativeStock));
        }
    }

    private async Task EditAsync(WarehouseStockRow row)
    {
        var dto = row.Source;
        using var form = new WarehouseStockEditForm("Edit Warehouse Stock", dto.MinimumStock, dto.MaximumStock, dto.AllowNegativeStock);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new SetWarehouseStockLevelsCommand(dto.WarehouseStockId, form.MinimumStock, form.MaximumStock));
            await _mediator.Send(new SetNegativeStockPolicyCommand(dto.WarehouseStockId, form.AllowNegativeStock));
        }
    }

    private async Task ReceiveInventoryAsync()
    {
        var variantOptions = _variantsById.Select(kv => (kv.Key, $"{kv.Value.Sku} - {kv.Value.Name}")).ToList();
        if (_warehouseOptions.Count == 0 || variantOptions.Count == 0)
        {
            XtraMessageBox.Show(this, "At least one warehouse and one product variant are required.", "Cannot Receive Inventory", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var form = new ReceiveInventoryForm(_warehouseOptions, _warehousePicker.SelectedId, variantOptions);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new OpenOrReceiveStockCommand(form.WarehouseId!.Value, form.ProductVariantId!.Value, form.Quantity, form.Notes));
            await _listView.RefreshAsync();
        }
    }

    private async Task ReceiveAsync(WarehouseStockRow row)
    {
        using var form = new QuantityPromptForm("Receive Stock", "Quantity to receive:");
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new ReceiveStockCommand(row.Source.WarehouseStockId, form.Quantity));
        }
    }

    private async Task IssueAsync(WarehouseStockRow row)
    {
        using var form = new QuantityPromptForm("Issue Stock", "Quantity to issue:");
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new IssueStockCommand(row.Source.WarehouseStockId, form.Quantity));
        }
    }

    private async Task ReserveAsync(WarehouseStockRow row)
    {
        using var form = new QuantityPromptForm("Reserve Stock", "Quantity to reserve:");
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new ReserveStockCommand(row.Source.WarehouseStockId, form.Quantity));
        }
    }

    private async Task ReleaseAsync(WarehouseStockRow row)
    {
        using var form = new QuantityPromptForm("Release Reservation", "Quantity to release:");
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new ReleaseStockReservationCommand(row.Source.WarehouseStockId, form.Quantity));
        }
    }

    /// <summary>Grid row shape: <see cref="WarehouseStockDto"/> enriched with the product's SKU/Name, resolved client-side the same way <c>RestaurantPosView</c> resolves its own product lookups - Application-layer DTOs stay cross-context-free.</summary>
    private sealed record WarehouseStockRow(
        string Sku,
        string Name,
        decimal QuantityOnHand,
        decimal QuantityReserved,
        decimal QuantityAvailable,
        decimal MinimumStock,
        decimal MaximumStock,
        bool AllowNegativeStock,
        DateTimeOffset UpdatedAtUtc,
        WarehouseStockDto Source);
}
