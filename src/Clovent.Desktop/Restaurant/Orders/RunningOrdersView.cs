using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Restaurant.Shared;
using Clovent.Desktop.Sessions;
using Clovent.Identity.Application.Authorization;
using Clovent.Restaurant.Application.Orders.Commands;
using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Application.Orders.Queries;
using Clovent.Restaurant.Application.Tables.Queries;
using Clovent.Restaurant.Orders;
using DevExpress.XtraEditors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Clovent.Desktop.Restaurant.Orders;

/// <summary>
/// Running Orders screen: monitors every currently open order across tables, take-away,
/// and delivery orders with lifecycle actions (Kitchen, Preparing, Ready, Rider, Delivered, Void, Cancel).
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

        if (DesignModeHelper.IsInDesignMode)
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
            _scope?.Dispose();
            _gate?.Dispose();
        }

        base.Dispose(disposing);
    }

    private async Task<IReadOnlyList<OrderRow>> LoadItemsAsync(CancellationToken cancellationToken)
    {
        var tables = await _mediator.Send(new ListAllTablesQuery(), cancellationToken);
        _tableCodesById = tables.ToDictionary(t => t.TableId, t => t.Code);

        var orders = await _mediator.Send(new ListOpenOrdersQuery(), cancellationToken);

        var filter = _comboOrderTypeFilter.SelectedItem?.ToString();
        if (!string.IsNullOrWhiteSpace(filter) && filter != "All Orders")
        {
            orders = orders.Where(o => string.Equals(o.OrderType, filter, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return [.. orders.Select(ToRow)];
    }

    private OrderRow ToRow(OrderDto order)
    {
        var tableDisplay = order.TableId is { } tableId ? _tableCodesById.GetValueOrDefault(tableId, "-") : "-";
        var custDisplay = !string.IsNullOrWhiteSpace(order.DeliveryCustomerName)
            ? $"{order.DeliveryCustomerName} ({order.DeliveryPhone ?? string.Empty})".Trim()
            : "-";

        return new OrderRow(
            order.OrderId,
            order.OrderNumber,
            order.OrderType,
            tableDisplay,
            order.OrderLineIds.Count,
            order.Notes ?? string.Empty,
            order.CreatedAtUtc,
            order.DeliveryStatus,
            order.RiderName ?? "-",
            custDisplay,
            order.OrderSource);
    }

    private async Task SetPreparingAsync(OrderRow row)
    {
        var source = Enum.TryParse<OrderSource>(row.OrderSource, out var s) ? s : OrderSource.Phone;
        await _mediator.Send(new UpdateDeliveryDetailsCommand(
            row.OrderId,
            source,
            null,
            null,
            null,
            null,
            0m,
            row.RiderName == "-" ? null : row.RiderName,
            RiderPhone: null,
            DeliveryStatus: DeliveryStatus.Preparing));
        await _listView.RefreshAsync();
    }

    private async Task SetReadyAsync(OrderRow row)
    {
        var source = Enum.TryParse<OrderSource>(row.OrderSource, out var s) ? s : OrderSource.Phone;
        await _mediator.Send(new UpdateDeliveryDetailsCommand(
            row.OrderId,
            source,
            null,
            null,
            null,
            null,
            0m,
            row.RiderName == "-" ? null : row.RiderName,
            RiderPhone: null,
            DeliveryStatus: DeliveryStatus.Ready));
        await _listView.RefreshAsync();
    }

    private async Task AssignRiderAsync(OrderRow row)
    {
        using var form = new TextPromptForm("Assign Rider", "Enter Rider / Courier Name:", initialText: row.RiderName == "-" ? string.Empty : row.RiderName, required: true);
        if (form.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(form.Value))
        {
            var source = Enum.TryParse<OrderSource>(row.OrderSource, out var s) ? s : OrderSource.Phone;
            await _mediator.Send(new UpdateDeliveryDetailsCommand(
                row.OrderId,
                source,
                null,
                null,
                null,
                null,
                0m,
                form.Value.Trim(),
                RiderPhone: null,
                DeliveryStatus: DeliveryStatus.OutForDelivery));
            await _listView.RefreshAsync();
        }
    }

    private async Task SetDeliveredAsync(OrderRow row)
    {
        var source = Enum.TryParse<OrderSource>(row.OrderSource, out var s) ? s : OrderSource.Phone;
        await _mediator.Send(new UpdateDeliveryDetailsCommand(
            row.OrderId,
            source,
            null,
            null,
            null,
            null,
            0m,
            row.RiderName == "-" ? null : row.RiderName,
            RiderPhone: null,
            DeliveryStatus: DeliveryStatus.Delivered));
        await _listView.RefreshAsync();
    }

    private async Task VoidAsync(OrderRow row)
    {
        using var form = new TextPromptForm("Void Order", "Reason:", required: true);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new VoidOrderCommand(row.OrderId, form.Value!));
            await _listView.RefreshAsync();
        }
    }

    private async Task CancelAsync(OrderRow row)
    {
        using var form = new TextPromptForm("Cancel Order", "Reason:", required: true);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            await _mediator.Send(new CancelOrderCommand(row.OrderId, form.Value!));
            await _listView.RefreshAsync();
        }
    }

    private Task<bool> CanUseFeatureAsync(string operation) =>
        _currentSession.UserId is { } userId
            ? _featurePolicy.CanUseFeatureAsync(userId, $"{FeatureCode}.{operation}")
            : Task.FromResult(false);

    private sealed record OrderRow(
        Guid OrderId,
        string OrderNumber,
        string OrderType,
        string TableCode,
        int LineCount,
        string Notes,
        DateTimeOffset CreatedAtUtc,
        string DeliveryStatus,
        string RiderName,
        string CustomerInfo,
        string OrderSource);
}
