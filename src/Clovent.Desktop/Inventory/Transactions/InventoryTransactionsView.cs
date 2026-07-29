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
/// Inventory Transactions screen: a read-only, append-only ledger view for a
/// selected warehouse - no New/Edit/Activate/Deactivate, since
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
    private readonly MasterDataListView<InventoryTransactionDto> _listView;

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public InventoryTransactionsView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _featurePolicy = _scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>();
        _currentSession = currentSession;

        Dock = DockStyle.Fill;

        _listView = new MasterDataListView<InventoryTransactionDto>(
        [
            new MasterDataColumn(nameof(InventoryTransactionDto.TransactionType), "Type", 90),
            new MasterDataColumn(nameof(InventoryTransactionDto.Quantity), "Quantity", 90),
            new MasterDataColumn(nameof(InventoryTransactionDto.ReferenceType), "Reference", 120),
            new MasterDataColumn(nameof(InventoryTransactionDto.Notes), "Notes", 220),
            new MasterDataColumn(nameof(InventoryTransactionDto.OccurredAtUtc), "Occurred (UTC)", 160),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = dto => $"{dto.TransactionType} {dto.ReferenceType} {dto.Notes}",
            CanUseFeatureAsync = operation => CanUseFeatureAsync(operation),
        };

        _warehousePicker.SelectionChanged += async (_, _) => await _listView.RefreshAsync();

        Controls.Add(_listView);
        Controls.Add(_warehousePicker);
        Load += async (_, _) => await LoadWarehousesAsync();
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

    private async Task LoadWarehousesAsync()
    {
        var warehouses = await _mediator.Send(new ListAllWarehousesQuery());
        _warehousePicker.LoadItems([.. warehouses.Select(w => (w.WarehouseId, w.Name))]);
    }

    private async Task<IReadOnlyList<InventoryTransactionDto>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        if (_warehousePicker.SelectedId is not { } warehouseId)
        {
            return [];
        }

        var items = await _mediator.Send(new ListInventoryTransactionsByWarehouseQuery(warehouseId), cancellationToken);
        return [.. items];
    }

    private Task<bool> CanUseFeatureAsync(string operation) =>
        _currentSession.UserId is { } userId
            ? _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}")
            : Task.FromResult(false);
}
