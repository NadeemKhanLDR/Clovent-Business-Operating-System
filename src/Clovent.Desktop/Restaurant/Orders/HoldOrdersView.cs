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
/// Hold Orders screen: every order currently on hold, with quick
/// Resume/Cancel actions. Mirrors <see cref="RunningOrdersView"/>'s shape -
/// a floor-wide overview, not a second place to edit an order's contents.
/// Feature-gated per <c>pos.{operation}</c>.
/// </summary>
[System.ComponentModel.DesignerCategory("Code")]
public sealed partial class HoldOrdersView : XtraUserControl
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
    public HoldOrdersView()
    {
        _scope = null!;
        _mediator = null!;
        _featurePolicy = null!;
        _currentSession = null!;

        InitializeComponent();
    }

    /// <summary>Builds the screen and starts its own DI scope for the Scoped services it needs.</summary>
    public HoldOrdersView(IServiceScopeFactory scopeFactory, ICurrentSession currentSession) : base()
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

    private async void HoldOrdersView_Load(object? sender, EventArgs e)
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
