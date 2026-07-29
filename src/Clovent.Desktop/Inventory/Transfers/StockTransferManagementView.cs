using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Desktop.MasterData;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Inventory.Application.Transfers.Commands;
using Clovent.Inventory.Application.Transfers.Dtos;
using Clovent.Inventory.Application.Transfers.Queries;
using Clovent.MasterData.Application.Warehouses.Queries;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Inventory.Transfers;

/// <summary>
/// Stock Transfer screen: search, filter, create, and Complete/Cancel over
/// warehouse-to-warehouse stock movements. Feature-gated per
/// <c>stocktransfers.{create|complete|cancel}</c>.
/// </summary>
public sealed class StockTransferManagementView : XtraUserControl
{
    private const string FeatureCode = "stocktransfers";

    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;
    private readonly MasterDataListView<StockTransferRow> _listView;

    private Dictionary<Guid, (string Sku, string Name)> _variantsById = [];
    private Dictionary<Guid, string> _warehouseNamesById = [];

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public StockTransferManagementView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _featurePolicy = _scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>();
        _currentSession = currentSession;

        Dock = DockStyle.Fill;

        _listView = new MasterDataListView<StockTransferRow>(
        [
            new MasterDataColumn(nameof(StockTransferRow.Sku), "SKU", 100),
            new MasterDataColumn(nameof(StockTransferRow.ProductName), "Product", 160),
            new MasterDataColumn(nameof(StockTransferRow.SourceWarehouseName), "From", 130),
            new MasterDataColumn(nameof(StockTransferRow.DestinationWarehouseName), "To", 130),
            new MasterDataColumn(nameof(StockTransferRow.Quantity), "Quantity", 90),
            new MasterDataColumn(nameof(StockTransferRow.Status), "Status", 90),
            new MasterDataColumn(nameof(StockTransferRow.CreatedAtUtc), "Created (UTC)", 160),
            new MasterDataColumn(nameof(StockTransferRow.CompletedAtUtc), "Completed (UTC)", 160),
        ],
        [
            new MasterDataListAction<StockTransferRow>("Complete", row => _mediator.Send(new CompleteStockTransferCommand(row.Source.StockTransferId)), row => row.Status == "Pending", "complete"),
            new MasterDataListAction<StockTransferRow>("Cancel", row => _mediator.Send(new CancelStockTransferCommand(row.Source.StockTransferId)), row => row.Status == "Pending", "cancel"),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = row => $"{row.Sku} {row.ProductName} {row.SourceWarehouseName} {row.DestinationWarehouseName}",
            CanUseFeatureAsync = operation => CanUseFeatureAsync(operation),
            OnNew = CreateAsync,
        };

        Controls.Add(_listView);
        Load += async (_, _) => await _listView.RefreshAsync();
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

    private async Task<IReadOnlyList<StockTransferRow>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        var warehouses = await _mediator.Send(new ListAllWarehousesQuery(), cancellationToken);
        _warehouseNamesById = warehouses.ToDictionary(w => w.WarehouseId, w => w.Name);

        var variants = await _mediator.Send(new ListProductVariantsQuery(), cancellationToken);
        _variantsById = variants.ToDictionary(v => v.ProductVariantId, v => (v.Sku, v.Name));

        var items = await _mediator.Send(new ListStockTransfersQuery(), cancellationToken);
        return
        [
            .. items.Select(dto =>
            {
                var (sku, name) = _variantsById.TryGetValue(dto.ProductVariantId, out var variant) ? variant : (string.Empty, string.Empty);
                var sourceName = _warehouseNamesById.GetValueOrDefault(dto.SourceWarehouseId, string.Empty);
                var destinationName = _warehouseNamesById.GetValueOrDefault(dto.DestinationWarehouseId, string.Empty);
                return new StockTransferRow(sku, name, sourceName, destinationName, dto.Quantity, dto.Status, dto.CreatedAtUtc, dto.CompletedAtUtc, dto);
            }),
        ];
    }

    private Task<bool> CanUseFeatureAsync(string operation) =>
        _currentSession.UserId is { } userId
            ? _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}")
            : Task.FromResult(false);

    private async Task CreateAsync()
    {
        var warehouses = await _mediator.Send(new ListAllWarehousesQuery());
        var warehouseOptions = warehouses.Select(w => (w.WarehouseId, w.Name)).ToList();
        if (warehouseOptions.Count < 2)
        {
            XtraMessageBox.Show(this, "At least two warehouses are required to transfer stock.", "Not Enough Warehouses", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var variants = await _mediator.Send(new ListProductVariantsQuery());
        var variantOptions = variants.Select(v => (v.ProductVariantId, $"{v.Sku} - {v.Name}")).ToList();
        if (variantOptions.Count == 0)
        {
            XtraMessageBox.Show(this, "Create a product variant first.", "No Variants Available", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var form = new StockTransferCreateForm(warehouseOptions, variantOptions);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new CreateStockTransferCommand(form.SourceWarehouseId!.Value, form.DestinationWarehouseId!.Value, form.VariantId!.Value, form.Quantity));
        }
    }

    /// <summary>Grid row shape: <see cref="StockTransferDto"/> enriched with product SKU/Name and warehouse names, resolved client-side (same reasoning as <c>WarehouseStockManagementView.WarehouseStockRow</c>).</summary>
    private sealed record StockTransferRow(
        string Sku,
        string ProductName,
        string SourceWarehouseName,
        string DestinationWarehouseName,
        decimal Quantity,
        string Status,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? CompletedAtUtc,
        StockTransferDto Source);
}
