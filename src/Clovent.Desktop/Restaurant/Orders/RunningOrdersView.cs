using Clovent.Desktop.Forms.Base;
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
/// Running Orders screen: monitors every currently open order across every
/// table and take-away, with quick Hold/Send to Kitchen/Void/Cancel actions.
/// Item-level work (adding lines, discounts, payment) stays in
/// <see cref="RestaurantPosForm"/> - this screen is a floor-wide overview,
/// not a second place to edit an order's contents. Feature-gated per
/// <c>pos.{operation}</c>, the same codes the POS screen itself uses since
/// these are the same order-lifecycle actions.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed partial class RunningOrdersView : XtraUserControl
{
    private const string FeatureCode = "pos";

    private readonly IServiceScope _scope;
    private readonly ScreenOperationGate _gate = new();
    private readonly IMediator _mediator;
    private readonly IFeatureAuthorizationPolicy _featurePolicy;
    private readonly ICurrentSession _currentSession;
    private Dictionary<Guid, string> _tableCodesById = [];
    /// <summary>Design-time-only constructor for the Visual Studio WinForms Designer - never used at runtime.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [Obsolete("Designer only", true)]
    public RunningOrdersView()
    {
        _scope = null!;
        _mediator = null!;
        _featurePolicy = null!;
        _currentSession = null!;

        InitializeComponent();
    }

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public RunningOrdersView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession) : base()
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

    private async void RunningOrdersView_Load(object? sender, EventArgs e)
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
