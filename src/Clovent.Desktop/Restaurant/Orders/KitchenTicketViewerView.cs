using Clovent.Desktop.MasterData;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Restaurant.Application.KitchenTickets.Commands;
using Clovent.Restaurant.Application.KitchenTickets.Dtos;
using Clovent.Restaurant.Application.KitchenTickets.Queries;
using Clovent.Restaurant.Application.Orders.Queries;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>
/// Kitchen Ticket Viewer: every ticket not yet served or cancelled, with
/// Start/Mark Ready/Serve/Cancel actions - the kitchen-facing counterpart to
/// <see cref="RunningOrdersView"/>'s front-of-house overview.
/// Feature-gated per <c>kitchentickets.{operation}</c>.
/// </summary>
public sealed class KitchenTicketViewerView : XtraUserControl
{
    private const string FeatureCode = "kitchentickets";

    private readonly IServiceScope _scope;
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;
    private readonly MasterDataListView<KitchenTicketRow> _listView;

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public KitchenTicketViewerView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession)
    {
        _scope = scopeFactory.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _featurePolicy = _scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>();
        _currentSession = currentSession;

        Dock = DockStyle.Fill;

        _listView = new MasterDataListView<KitchenTicketRow>(
        [
            new MasterDataColumn(nameof(KitchenTicketRow.OrderNumber), "Order #", 140),
            new MasterDataColumn(nameof(KitchenTicketRow.LineCount), "Lines", 60),
            new MasterDataColumn(nameof(KitchenTicketRow.Status), "Status", 90),
            new MasterDataColumn(nameof(KitchenTicketRow.CreatedAtUtc), "Sent (UTC)", 160),
            new MasterDataColumn(nameof(KitchenTicketRow.StartedAtUtc), "Started (UTC)", 160),
            new MasterDataColumn(nameof(KitchenTicketRow.ReadyAtUtc), "Ready (UTC)", 160),
        ],
        [
            new MasterDataListAction<KitchenTicketRow>("Start", row => _mediator.Send(new StartKitchenTicketCommand(row.KitchenTicketId)),
                row => row.Status == "New", "start"),
            new MasterDataListAction<KitchenTicketRow>("Mark Ready", row => _mediator.Send(new MarkKitchenTicketReadyCommand(row.KitchenTicketId)),
                row => row.Status == "InProgress", "markready"),
            new MasterDataListAction<KitchenTicketRow>("Serve", row => _mediator.Send(new ServeKitchenTicketCommand(row.KitchenTicketId)),
                row => row.Status == "Ready", "serve"),
            new MasterDataListAction<KitchenTicketRow>("Cancel", row => _mediator.Send(new CancelKitchenTicketCommand(row.KitchenTicketId)),
                row => row.Status is "New" or "InProgress", "cancel"),
        ])
        {
            LoadItemsAsync = LoadItemsAsync,
            SearchTextSelector = row => row.OrderNumber,
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

    private async Task<IReadOnlyList<KitchenTicketRow>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        var tickets = await _mediator.Send(new ListActiveKitchenTicketsQuery(), cancellationToken);
        var rows = new List<KitchenTicketRow>();

        foreach (var ticket in tickets)
        {
            var order = await _mediator.Send(new GetOrderByIdQuery(ticket.OrderId), cancellationToken);
            rows.Add(ToRow(ticket, order.OrderNumber));
        }

        return rows;
    }

    private static KitchenTicketRow ToRow(KitchenTicketDto ticket, string orderNumber) => new(
        ticket.KitchenTicketId,
        orderNumber,
        ticket.OrderLineIds.Count,
        ticket.Status,
        ticket.CreatedAtUtc,
        ticket.StartedAtUtc,
        ticket.ReadyAtUtc);

    private Task<bool> CanUseFeatureAsync(string operation) =>
        _currentSession.UserId is { } userId
            ? _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}")
            : Task.FromResult(false);

    private sealed record KitchenTicketRow(
        Guid KitchenTicketId,
        string OrderNumber,
        int LineCount,
        string Status,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? StartedAtUtc,
        DateTimeOffset? ReadyAtUtc);
}
