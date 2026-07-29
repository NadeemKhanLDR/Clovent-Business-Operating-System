using Clovent.Desktop.MasterData;
using Clovent.Desktop.Restaurant.Shared;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Restaurant.Application.Orders.Commands;
using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Application.Orders.Queries;
using Clovent.Restaurant.Application.Tables.Queries;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>
/// Hold Orders screen: every order currently on hold, with quick
/// Resume/Cancel actions. Mirrors <see cref="RunningOrdersView"/>'s shape -
/// a floor-wide overview, not a second place to edit an order's contents.
/// Feature-gated per <c>pos.{operation}</c>.
/// </summary>
public sealed class HoldOrdersView : XtraUserControl
{
    private const string FeatureCode = "pos";

    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;
    private readonly MasterDataListView<HeldOrderRow> _listView;
    private Dictionary<Guid, string> _tableCodesById = [];

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public HoldOrdersView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _featurePolicy = _scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>();
        _currentSession = currentSession;

        Dock = DockStyle.Fill;

        _listView = new MasterDataListView<HeldOrderRow>(
        [
            new MasterDataColumn(nameof(HeldOrderRow.OrderNumber), "Order #", 140),
            new MasterDataColumn(nameof(HeldOrderRow.OrderType), "Type", 90),
            new MasterDataColumn(nameof(HeldOrderRow.TableCode), "Table", 90),
            new MasterDataColumn(nameof(HeldOrderRow.LineCount), "Lines", 60),
            new MasterDataColumn(nameof(HeldOrderRow.Notes), "Notes", 200),
            new MasterDataColumn(nameof(HeldOrderRow.UpdatedAtUtc), "Held Since (UTC)", 160),
        ],
        [
            new MasterDataListAction<HeldOrderRow>("Resume", row => _mediator.Send(new ResumeOrderCommand(row.OrderId)), FeatureOperation: "resume"),
            new MasterDataListAction<HeldOrderRow>("Cancel", CancelAsync, FeatureOperation: "cancel"),
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

    private async Task<IReadOnlyList<HeldOrderRow>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        var tables = await _mediator.Send(new ListAllTablesQuery(), cancellationToken);
        _tableCodesById = tables.ToDictionary(t => t.TableId, t => t.Code);

        var orders = await _mediator.Send(new ListHeldOrdersQuery(), cancellationToken);
        return [.. orders.Select(ToRow)];
    }

    private HeldOrderRow ToRow(OrderDto order) => new(
        order.OrderId,
        order.OrderNumber,
        order.OrderType,
        order.TableId is { } tableId ? _tableCodesById.GetValueOrDefault(tableId, "-") : "-",
        order.OrderLineIds.Count,
        order.Notes ?? string.Empty,
        order.UpdatedAtUtc);

    private async Task CancelAsync(HeldOrderRow row)
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

    private sealed record HeldOrderRow(Guid OrderId, string OrderNumber, string OrderType, string TableCode, int LineCount, string Notes, DateTimeOffset UpdatedAtUtc);
}
