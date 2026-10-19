using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Desktop.Forms.Base;
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
/// <c>inventorytransactions.view</c>. Control tree (list view, warehouse/
/// product filters) lives in <c>InventoryTransactionsView.Designer.cs</c>;
/// this file holds behavior only.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed partial class InventoryTransactionsView : XtraUserControl
{
    private const string FeatureCode = "inventorytransactions";

    private readonly IServiceScope _scope;
    private readonly ScreenOperationGate _gate = new();
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;
    private readonly CancellationTokenSource _lifecycleCts = new();

    private Dictionary<Guid, (string Sku, string Name)> _variantsById = [];
    private bool _isLoadingPickers;

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public InventoryTransactionsView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = new SerializedMediator(_scope.ServiceProvider.GetRequiredService<IMediator>(), _gate);
        _featurePolicy = new SerializedFeatureAuthorizationPolicy(_scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>(), _gate);
        _currentSession = currentSession;

        InitializeComponent();
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

    private async void WarehousePicker_SelectionChanged(object? sender, EventArgs e)
    {
        if (_isLoadingPickers || _lifecycleCts.IsCancellationRequested || IsDisposed || Disposing)
        {
            return;
        }

        await _listView.RefreshAsync(_lifecycleCts.Token);
    }

    private async void ProductPicker_SelectionChanged(object? sender, EventArgs e)
    {
        if (_isLoadingPickers || _lifecycleCts.IsCancellationRequested || IsDisposed || Disposing)
        {
            return;
        }

        await _listView.RefreshAsync(_lifecycleCts.Token);
    }

    private async void InventoryTransactionsView_Load(object? sender, EventArgs e) => await LoadPickersAsync();

    private async Task LoadPickersAsync()
    {
        _isLoadingPickers = true;
        try
        {
            var warehouses = await _mediator.Send(new ListAllWarehousesQuery(), _lifecycleCts.Token);
            _warehousePicker.LoadItems([.. warehouses.Select(w => (w.WarehouseId, w.Name))]);

            var variants = await _mediator.Send(new ListProductVariantsQuery(), _lifecycleCts.Token);
            _variantsById = variants.ToDictionary(v => v.ProductVariantId, v => (v.Sku, v.Name));
            _productPicker.LoadItems([.. variants.Select(v => (v.ProductVariantId, $"{v.Sku} - {v.Name}"))]);
        }
        catch (OperationCanceledException) when (_lifecycleCts.IsCancellationRequested)
        {
            return;
        }
        finally
        {
            _isLoadingPickers = false;
        }

        if (!_lifecycleCts.IsCancellationRequested && !IsDisposed && !Disposing)
        {
            await _listView.RefreshAsync(_lifecycleCts.Token);
        }
    }

    private async Task<IReadOnlyList<InventoryTransactionRow>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifecycleCts.Token);
        var token = linked.Token;
        IReadOnlyCollection<InventoryTransactionDto> items;

        if (_productPicker.SelectedId is { } productId)
        {
            items = await _mediator.Send(new ListInventoryTransactionsByProductQuery(productId), token);
        }
        else if (_warehousePicker.SelectedId is { } warehouseId)
        {
            items = await _mediator.Send(new ListInventoryTransactionsByWarehouseQuery(warehouseId), token);
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
        _currentSession.UserId is { } userId && !_lifecycleCts.IsCancellationRequested && !IsDisposed && !Disposing
            ? _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}", _lifecycleCts.Token)
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
