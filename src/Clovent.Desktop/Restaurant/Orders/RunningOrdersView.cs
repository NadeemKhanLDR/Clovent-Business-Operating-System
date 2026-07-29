using Clovent.Desktop.MasterData;
using Clovent.Desktop.Restaurant.Shared;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Restaurant.Application.KitchenTickets.Commands;
using Clovent.Restaurant.Application.Orders.Commands;
using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Application.Orders.Queries;
using Clovent.Restaurant.Application.Tables.Queries;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>
/// Running Orders screen: monitors every currently open order across every
/// table and take-away, with quick Hold/Send to Kitchen/Void/Cancel actions.
/// Item-level work (adding lines, discounts, payment) stays in
/// <see cref="RestaurantPosView"/> - this screen is a floor-wide overview,
/// not a second place to edit an order's contents. Feature-gated per
/// <c>pos.{operation}</c>, the same codes the POS screen itself uses since
/// these are the same order-lifecycle actions.
/// </summary>
public sealed class RunningOrdersView : XtraUserControl
{
    private const string FeatureCode = "pos";

    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;
    private readonly MasterDataListView<OrderRow> _listView;
    private Dictionary<Guid, string> _tableCodesById = [];

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public RunningOrdersView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _featurePolicy = _scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>();
        _currentSession = currentSession;

        Dock = DockStyle.Fill;

        _listView = new MasterDataListView<OrderRow>(
        [
            new MasterDataColumn(nameof(OrderRow.OrderNumber), "Order #", 140),
            new MasterDataColumn(nameof(OrderRow.OrderType), "Type", 90),
            new MasterDataColumn(nameof(OrderRow.TableCode), "Table", 90),
            new MasterDataColumn(nameof(OrderRow.LineCount), "Lines", 60),
            new MasterDataColumn(nameof(OrderRow.Notes), "Notes", 200),
            new MasterDataColumn(nameof(OrderRow.CreatedAtUtc), "Opened (UTC)", 160),
        ],
        [
            new MasterDataListAction<OrderRow>("Hold", row => _mediator.Send(new HoldOrderCommand(row.OrderId)), FeatureOperation: "hold"),
            new MasterDataListAction<OrderRow>("Send to Kitchen", row => _mediator.Send(new SendOrderToKitchenCommand(row.OrderId)), FeatureOperation: "sendtokitchen"),
            new MasterDataListAction<OrderRow>("Void", VoidAsync, FeatureOperation: "void"),
            new MasterDataListAction<OrderRow>("Cancel", CancelAsync, FeatureOperation: "cancel"),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = row => $"{row.OrderNumber} {row.TableCode}",
            CanUseFeatureAsync = operation => CanUseFeatureAsync(operation),
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

    private async Task<IReadOnlyList<OrderRow>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        var tables = await _mediator.Send(new ListAllTablesQuery(), cancellationToken);
        _tableCodesById = tables.ToDictionary(t => t.TableId, t => t.Code);

        var orders = await _mediator.Send(new ListOpenOrdersQuery(), cancellationToken);
        return [.. orders.Select(ToRow)];
    }

    private OrderRow ToRow(OrderDto order) => new(
        order.OrderId,
        order.OrderNumber,
        order.OrderType,
        order.TableId is { } tableId ? _tableCodesById.GetValueOrDefault(tableId, "-") : "-",
        order.OrderLineIds.Count,
        order.Notes ?? string.Empty,
        order.CreatedAtUtc);

    private async Task VoidAsync(OrderRow row)
    {
        using var form = new TextPromptForm("Void Order", "Reason:", required: true);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new VoidOrderCommand(row.OrderId, form.Value!));
        }
    }

    private async Task CancelAsync(OrderRow row)
    {
        using var form = new TextPromptForm("Cancel Order", "Reason:", required: true);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new CancelOrderCommand(row.OrderId, form.Value!));
        }
    }

    private Task<bool> CanUseFeatureAsync(string operation) =>
        _currentSession.UserId is { } userId
            ? _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}")
            : Task.FromResult(false);

    private sealed record OrderRow(Guid OrderId, string OrderNumber, string OrderType, string TableCode, int LineCount, string Notes, DateTimeOffset CreatedAtUtc);
}
