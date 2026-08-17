using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Application.Orders.Queries;
using Clovent.Restaurant.Application.Tables.Queries;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>
/// Order History screen: looks up orders that have already closed
/// (Completed/Cancelled/Voided) and opens the existing
/// <see cref="PaymentHistoryDialog"/> against one of them.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this screen exists.</b> Every route into
/// <see cref="PaymentHistoryDialog"/> ran through
/// <see cref="RestaurantPosForm"/>, whose only way of loading an existing
/// order is <c>GetOpenOrHeldOrderByTableQuery</c> - Open and Held only.
/// Running Orders lists open orders and Held Orders lists held ones, so once
/// an order closed, its payments became unreachable: a payment recorded in
/// error on a completed or cancelled bill could never be voided, however
/// plainly wrong it was. Orders ORD-35, ORD-4 and ORD-2 are all in exactly
/// that state.
/// </para>
/// <para>
/// <b>What it deliberately does not do.</b> It is a lookup, not a second POS.
/// It cannot reopen, re-complete, edit or re-price an order, and it issues no
/// inventory movement - the only thing it can change is a payment, through
/// <c>VoidPaymentCommand</c>, which the dialog it opens already owns. That
/// restraint is not incidental: re-completing a historical order would issue
/// its stock a second time (those orders' inventory transactions predate
/// reference stamping, so the idempotency check in
/// <c>CompleteOrderCommandHandler</c> cannot see them) and would then fail on
/// <c>AssignDailySalesNumber</c>, leaving the order stuck. There is
/// intentionally no code path here that could start that sequence.
/// </para>
/// </remarks>
[System.ComponentModel.DesignerCategory("Code")]
public sealed partial class OrderHistoryView : XtraUserControl
{
    private const string FeatureCode = "pos";

    private readonly IServiceScope _scope;
    private readonly ScreenOperationGate _gate = new();
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;
    private readonly ILogger<OrderHistoryView> _logger;
    private Dictionary<Guid, string> _tableCodesById = [];

    /// <summary>Design-time-only constructor for the Visual Studio WinForms Designer - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public OrderHistoryView()
    {
        _scope = null!;
        _mediator = null!;
        _featurePolicy = null!;
        _currentSession = null!;
        _logger = null!;

        InitializeComponent();
    }

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs, matching <see cref="RunningOrdersView"/>.</summary>
    public OrderHistoryView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession, ILogger<OrderHistoryView> logger) : base()
    {
        InitializeComponent();

        if (DesignModeHelper.IsInDesignMode)
        {
            _scope = null!;
            _mediator = null!;
            _featurePolicy = null!;
            _currentSession = null!;
            _logger = null!;
            return;
        }

        _scope = scopeFactory.CreateScope();
        _mediator = new SerializedMediator(_scope.ServiceProvider.GetRequiredService<IMediator>(), _gate);
        _featurePolicy = new SerializedFeatureAuthorizationPolicy(_scope.ServiceProvider.GetRequiredService<IFeatureAuthorizationPolicy>(), _gate);
        _currentSession = currentSession;
        _logger = logger;
    }

    private async void OrderHistoryView_Load(object? sender, EventArgs e)
    {
        if (DesignModeHelper.IsInDesignMode)
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

    /// <remarks>
    /// Totals come from <c>GetOrderSummaryQuery</c> - the same figures the POS
    /// and the receipt show, rather than a second arithmetic - which costs one
    /// query per listed order. That is fine at the current scale and keeps this
    /// screen free of its own totals logic; if closed-order history grows large
    /// enough for the load to be felt, the fix is a projection query in the
    /// Application layer, not a private calculation here.
    /// </remarks>
    private async Task<IReadOnlyList<ClosedOrderRow>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        var tables = await _mediator.Send(new ListAllTablesQuery(), cancellationToken);
        _tableCodesById = tables.ToDictionary(t => t.TableId, t => t.Code);

        var orders = await _mediator.Send(new ListAllOrdersQuery(), cancellationToken);
        var closed = orders.Where(o => OrderHistoryRules.IsClosed(o.Status)).OrderByDescending(o => o.UpdatedAtUtc).ToList();

        var rows = new List<ClosedOrderRow>(closed.Count);
        foreach (var order in closed)
        {
            var totals = await _mediator.Send(new GetOrderSummaryQuery(order.OrderId), cancellationToken);
            rows.Add(ToRow(order, totals.GrandTotal, totals.PaidTotal, totals.Balance));
        }

        return rows;
    }

    private ClosedOrderRow ToRow(OrderDto order, decimal grandTotal, decimal paidTotal, decimal balance) => new(
        order.OrderId,
        order.OrderNumber,
        order.DailySalesNumber,
        order.Status,
        order.OrderType,
        order.TableId is { } tableId ? _tableCodesById.GetValueOrDefault(tableId, "-") : "-",
        grandTotal,
        paidTotal,
        balance,
        order.PaymentIds.Count,
        order.CreatedAtUtc,
        order.UpdatedAtUtc);

    /// <summary>
    /// Opens the existing <see cref="PaymentHistoryDialog"/> for the selected
    /// closed order - the same dialog, the same <c>VoidPaymentCommand</c> and
    /// the same "Refund" activity log entry the POS route already uses. No
    /// payment logic is duplicated here.
    /// </summary>
    private async Task ShowPaymentHistoryAsync(ClosedOrderRow row)
    {
        using var dialog = new PaymentHistoryDialog(_mediator, _logger, row.OrderId, _currentSession.DisplayName ?? "Unknown");
        dialog.ShowDialog(this);

        // A void changes this screen's Paid/Balance columns, so re-read after
        // the dialog closes. The order's own status is untouched by voiding a
        // payment and is re-read here only because the whole row is.
        await _listView.RefreshAsync();
    }

    private Task<bool> CanUseFeatureAsync(string operation) =>
        _currentSession.UserId is { } userId
            ? _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}")
            : Task.FromResult(false);

    /// <summary>One closed order as listed by this screen.</summary>
    private sealed record ClosedOrderRow(
        Guid OrderId,
        string OrderNumber,
        int? DailySalesNumber,
        string Status,
        string OrderType,
        string TableCode,
        decimal GrandTotal,
        decimal PaidTotal,
        decimal Balance,
        int PaymentCount,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc);
}
