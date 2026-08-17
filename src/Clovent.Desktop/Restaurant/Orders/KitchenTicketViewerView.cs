using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
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
[System.ComponentModel.DesignerCategory("Code")]
public sealed partial class KitchenTicketViewerView : XtraUserControl
{
    private const string FeatureCode = "kitchentickets";

    private readonly IServiceScope _scope;
    private readonly ScreenOperationGate _gate = new();
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;
    /// <summary>Design-time-only constructor for the Visual Studio WinForms Designer - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public KitchenTicketViewerView()
    {
        _scope = null!;
        _mediator = null!;
        _featurePolicy = null!;
        _currentSession = null!;

        InitializeComponent();
    }

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public KitchenTicketViewerView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession) : base()
    {
        InitializeComponent();

        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
        {
            _scope = null!;
            _mediator = null!;
            _featurePolicy = null!;
            _currentSession = null!;
            return;
        }

        _scope = scopeFactory.CreateScope();
        _mediator = new SerializedMediator(_scope.ServiceProvider.GetRequiredService<IMediator>(), _gate);
        _featurePolicy = new SerializedFeatureAuthorizationPolicy(_scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>(), _gate);
        _currentSession = currentSession;
    }
    private async void KitchenTicketViewerView_Load(object? sender, EventArgs e)
    {
        if (Clovent.Desktop.Forms.Base.DesignModeHelper.IsInDesignMode)
            return;
        await _listView.RefreshAsync();
    }
    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
            _scope.Dispose();
            _gate.Dispose();
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
