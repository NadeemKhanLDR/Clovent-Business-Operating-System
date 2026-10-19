using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Desktop.Forms.Base;
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
    private readonly ScreenOperationGate _gate = new();
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;
    private readonly CancellationTokenSource _lifecycleCts = new();
    private bool _isLoadingWarehouses;

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public StockAdjustmentManagementView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
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
        if (_isLoadingWarehouses || _lifecycleCts.IsCancellationRequested || IsDisposed || Disposing)
        {
            return;
        }

        await _listView.RefreshAsync(_lifecycleCts.Token);
    }

    private async void StockAdjustmentManagementView_Load(object? sender, EventArgs e) => await LoadWarehousesAsync();

    private async Task LoadWarehousesAsync()
    {
        _isLoadingWarehouses = true;
        try
        {
            var warehouses = await _mediator.Send(new ListAllWarehousesQuery(), _lifecycleCts.Token);
            _warehousePicker.LoadItems([.. warehouses.Select(w => (w.WarehouseId, w.Name))]);
        }
        catch (OperationCanceledException) when (_lifecycleCts.IsCancellationRequested)
        {
            return;
        }
        finally
        {
            _isLoadingWarehouses = false;
        }

        if (!_lifecycleCts.IsCancellationRequested && !IsDisposed && !Disposing)
        {
            await _listView.RefreshAsync(_lifecycleCts.Token);
        }
    }

    private async Task<IReadOnlyList<StockAdjustmentDto>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        if (_warehousePicker.SelectedId is not { } warehouseId)
        {
            return [];
        }

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifecycleCts.Token);
        var items = await _mediator.Send(new ListStockAdjustmentsByWarehouseQuery(warehouseId), linked.Token);
        return [.. items];
    }

    private Task<bool> CanUseFeatureAsync(string operation) =>
        _currentSession.UserId is { } userId && !_lifecycleCts.IsCancellationRequested && !IsDisposed && !Disposing
            ? _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}", _lifecycleCts.Token)
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
