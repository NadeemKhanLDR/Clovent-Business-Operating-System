using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Desktop.MasterData;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Inventory.Application.Transactions.Dtos;
using Clovent.Inventory.Application.Transactions.Queries;
using Clovent.MasterData.Application.Warehouses.Queries;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Inventory.Transactions;

/// <summary>
/// Inventory Transactions screen: a read-only, append-only ledger view,
/// scoped either by warehouse (the full ledger for one location) or by
/// product (that product's full "Stock History" across every warehouse) -
/// no New/Edit/Activate/Deactivate, since
/// <see cref="Clovent.Inventory.Transactions.InventoryTransaction"/> is only
/// ever created by the Warehouse Stock/Stock Adjustment/Stock Transfer
/// screens' own handlers, never directly. Feature-gated per
/// <c>inventorytransactions.view</c>.
/// </summary>
public sealed class InventoryTransactionsView : XtraUserControl
{
    private const string FeatureCode = "inventorytransactions";

    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;
    private readonly EntityPicker _warehousePicker = new("Warehouse:");
    private readonly EntityPicker _productPicker = new("Product (Stock History):");
    private readonly MasterDataListView<InventoryTransactionRow> _listView;

    private Dictionary<Guid, (string Sku, string Name)> _variantsById = [];

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public InventoryTransactionsView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _featurePolicy = _scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>();
        _currentSession = currentSession;

        Dock = DockStyle.Fill;

        _listView = new MasterDataListView<InventoryTransactionRow>(
        [
            new MasterDataColumn(nameof(InventoryTransactionRow.Sku), "SKU", 100),
            new MasterDataColumn(nameof(InventoryTransactionRow.ProductName), "Product", 160),
            new MasterDataColumn(nameof(InventoryTransactionRow.TransactionType), "Type", 100),
            new MasterDataColumn(nameof(InventoryTransactionRow.Quantity), "Quantity", 90),
            new MasterDataColumn(nameof(InventoryTransactionRow.ReferenceType), "Reference", 120),
            new MasterDataColumn(nameof(InventoryTransactionRow.Notes), "Notes", 200),
            new MasterDataColumn(nameof(InventoryTransactionRow.OccurredAtUtc), "Occurred (UTC)", 160),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = row => $"{row.Sku} {row.ProductName} {row.TransactionType} {row.ReferenceType} {row.Notes}",
            CanUseFeatureAsync = operation => CanUseFeatureAsync(operation),
        };

        // Product takes precedence when both are selected - see LoadItemsAsync.
        _warehousePicker.SelectionChanged += async (_, _) => await _listView.RefreshAsync();
        _productPicker.SelectionChanged += async (_, _) => await _listView.RefreshAsync();

        Controls.Add(_listView);
        Controls.Add(_productPicker);
        Controls.Add(_warehousePicker);
        Load += async (_, _) => await LoadPickersAsync();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _scope.Dispose();
        }

        base.Dispose(disposing);
    }

    private async Task LoadPickersAsync()
    {
        var warehouses = await _mediator.Send(new ListAllWarehousesQuery());
        _warehousePicker.LoadItems([.. warehouses.Select(w => (w.WarehouseId, w.Name))]);

        var variants = await _mediator.Send(new ListProductVariantsQuery());
        _variantsById = variants.ToDictionary(v => v.ProductVariantId, v => (v.Sku, v.Name));
        _productPicker.LoadItems([.. variants.Select(v => (v.ProductVariantId, $"{v.Sku} - {v.Name}"))]);
    }

    private async Task<IReadOnlyList<InventoryTransactionRow>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<InventoryTransactionDto> items;

        if (_productPicker.SelectedId is { } productId)
        {
            items = await _mediator.Send(new ListInventoryTransactionsByProductQuery(productId), cancellationToken);
        }
        else if (_warehousePicker.SelectedId is { } warehouseId)
        {
            items = await _mediator.Send(new ListInventoryTransactionsByWarehouseQuery(warehouseId), cancellationToken);
        }
        else
        {
            return [];
        }

        return
        [
            .. items
                .OrderByDescending(dto => dto.OccurredAtUtc)
                .Select(dto =>
                {
                    var (sku, name) = _variantsById.TryGetValue(dto.ProductVariantId, out var variant) ? variant : (string.Empty, string.Empty);
                    return new InventoryTransactionRow(sku, name, dto.TransactionType, dto.Quantity, dto.ReferenceType, dto.Notes, dto.OccurredAtUtc, dto);
                }),
        ];
    }

    private Task<bool> CanUseFeatureAsync(string operation) =>
        _currentSession.UserId is { } userId
            ? _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}")
            : Task.FromResult(false);

    /// <summary>Grid row shape: <see cref="InventoryTransactionDto"/> enriched with the product's SKU/Name, resolved client-side (same reasoning as <c>WarehouseStockManagementView.WarehouseStockRow</c>).</summary>
    private sealed record InventoryTransactionRow(
        string Sku,
        string ProductName,
        string TransactionType,
        decimal Quantity,
        string? ReferenceType,
        string? Notes,
        DateTimeOffset OccurredAtUtc,
        InventoryTransactionDto Source);
}
