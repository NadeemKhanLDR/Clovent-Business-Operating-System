using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Desktop.MasterData;
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
/// negative-stock policy), and Receive/Issue/Reserve/Release over the stock
/// balances of a selected warehouse. Feature-gated per
/// <c>warehousestocks.{create|edit|receive|issue|reserve|release}</c>.
/// </summary>
public sealed class WarehouseStockManagementView : XtraUserControl
{
    private const string FeatureCode = "warehousestocks";

    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;
    private readonly EntityPicker _warehousePicker = new("Warehouse:");
    private readonly MasterDataListView<WarehouseStockDto> _listView;

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public WarehouseStockManagementView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _featurePolicy = _scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>();
        _currentSession = currentSession;

        Dock = DockStyle.Fill;

        _listView = new MasterDataListView<WarehouseStockDto>(
        [
            new MasterDataColumn(nameof(WarehouseStockDto.QuantityOnHand), "On Hand", 90),
            new MasterDataColumn(nameof(WarehouseStockDto.QuantityReserved), "Reserved", 90),
            new MasterDataColumn(nameof(WarehouseStockDto.QuantityAvailable), "Available", 90),
            new MasterDataColumn(nameof(WarehouseStockDto.MinimumStock), "Min", 70),
            new MasterDataColumn(nameof(WarehouseStockDto.MaximumStock), "Max", 70),
            new MasterDataColumn(nameof(WarehouseStockDto.AllowNegativeStock), "Neg. OK", 70),
            new MasterDataColumn(nameof(WarehouseStockDto.UpdatedAtUtc), "Updated (UTC)", 160),
        ],
        [
            new MasterDataListAction<WarehouseStockDto>("Receive", ReceiveAsync, FeatureOperation: "receive"),
            new MasterDataListAction<WarehouseStockDto>("Issue", IssueAsync, FeatureOperation: "issue"),
            new MasterDataListAction<WarehouseStockDto>("Reserve", ReserveAsync, FeatureOperation: "reserve"),
            new MasterDataListAction<WarehouseStockDto>("Release", ReleaseAsync, FeatureOperation: "release"),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            CanUseFeatureAsync = operation => CanUseFeatureAsync(operation),
            OnNew = CreateAsync,
            OnEdit = EditAsync,
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

    private async Task<IReadOnlyList<WarehouseStockDto>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        if (_warehousePicker.SelectedId is not { } warehouseId)
        {
            return [];
        }

        var items = await _mediator.Send(new ListWarehouseStocksByWarehouseQuery(warehouseId), cancellationToken);
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

        using var form = new WarehouseStockEditForm("New Warehouse Stock", variantOptions);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new CreateWarehouseStockCommand(warehouseId, form.VariantId!.Value, form.MinimumStock, form.MaximumStock, form.AllowNegativeStock));
        }
    }

    private async Task EditAsync(WarehouseStockDto dto)
    {
        using var form = new WarehouseStockEditForm("Edit Warehouse Stock", dto.MinimumStock, dto.MaximumStock, dto.AllowNegativeStock);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new SetWarehouseStockLevelsCommand(dto.WarehouseStockId, form.MinimumStock, form.MaximumStock));
            await _mediator.Send(new SetNegativeStockPolicyCommand(dto.WarehouseStockId, form.AllowNegativeStock));
        }
    }

    private async Task ReceiveAsync(WarehouseStockDto dto)
    {
        using var form = new QuantityPromptForm("Receive Stock", "Quantity to receive:");
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new ReceiveStockCommand(dto.WarehouseStockId, form.Quantity));
        }
    }

    private async Task IssueAsync(WarehouseStockDto dto)
    {
        using var form = new QuantityPromptForm("Issue Stock", "Quantity to issue:");
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new IssueStockCommand(dto.WarehouseStockId, form.Quantity));
        }
    }

    private async Task ReserveAsync(WarehouseStockDto dto)
    {
        using var form = new QuantityPromptForm("Reserve Stock", "Quantity to reserve:");
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new ReserveStockCommand(dto.WarehouseStockId, form.Quantity));
        }
    }

    private async Task ReleaseAsync(WarehouseStockDto dto)
    {
        using var form = new QuantityPromptForm("Release Reservation", "Quantity to release:");
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new ReleaseStockReservationCommand(dto.WarehouseStockId, form.Quantity));
        }
    }
}
