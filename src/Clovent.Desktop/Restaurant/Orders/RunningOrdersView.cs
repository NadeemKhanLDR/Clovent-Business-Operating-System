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

        HandleCreated += (_, _) => ScaleFilter();
    }

    private void ComboOrderTypeFilter_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_listView != null && _mediator != null)
        {
            _ = _listView.RefreshAsync();
        }
    }

    private void ScaleFilter()
    {
        if (DesignModeHelper.IsInDesignMode) return;
        var comboWidth = DesktopDpi.Scale(180, this);
        var comboHeight = DesktopDpi.Scale(DesktopStyle.ToolbarControlHeight, this);
        _comboOrderTypeFilter.Size = new System.Drawing.Size(comboWidth, comboHeight);
        _comboOrderTypeFilter.MinimumSize = new System.Drawing.Size(comboWidth, comboHeight);
    }

    private async void RunningOrdersView_Load(object? sender, EventArgs e)
    {
        if (DesignModeHelper.IsInDesignMode)
            return;
        ScaleFilter();
        if (_mediator == null)
            return;
        await _listView.RefreshAsync();
    }

    /// <inheritdoc/>
    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        ScaleFilter();
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

    private async Task UpdateDeliveryStatusAsync(OrderRow row, DeliveryStatus newStatus, string? riderNameOverride = null)
    {
        var order = await _mediator.Send(new GetOrderByIdQuery(row.OrderId));
        var source = Enum.TryParse<OrderSource>(order.OrderSource, out var s) ? s : OrderSource.Phone;
        var riderName = riderNameOverride ?? order.RiderName;

        await _mediator.Send(new UpdateDeliveryDetailsCommand(
            row.OrderId,
            source,
            order.DeliveryCustomerName,
            order.DeliveryPhone,
            order.DeliveryAddress,
            order.DeliveryNotes,
            order.DeliveryFee,
            riderName,
            order.RiderPhone,
            DeliveryStatus: newStatus));
        await _listView.RefreshAsync();
    }

    private Task SetPreparingAsync(OrderRow row) =>
        UpdateDeliveryStatusAsync(row, DeliveryStatus.Preparing);

    private Task SetReadyAsync(OrderRow row) =>
        UpdateDeliveryStatusAsync(row, DeliveryStatus.Ready);

    private async Task AssignRiderAsync(OrderRow row)
    {
        using var form = new TextPromptForm("Assign Rider", "Enter Rider / Courier Name:", initialText: row.RiderName == "-" ? string.Empty : row.RiderName, required: true);
        if (form.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(form.Value))
        {
            await UpdateDeliveryStatusAsync(row, DeliveryStatus.OutForDelivery, form.Value.Trim());
        }
    }

    private Task SetDeliveredAsync(OrderRow row) =>
        UpdateDeliveryStatusAsync(row, DeliveryStatus.Delivered);

    private async Task VoidAsync(OrderRow row)
    {
        using var form = new TextPromptForm("Void Order", "Reason:", required: true);
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var managerAuthService = _scope.ServiceProvider.GetService<Clovent.Desktop.Authorization.IManagerAuthorizationService>();
        string? prefilledUsername = null;
        if (_currentSession.UserId is { } uid && await _featurePolicy.CanUseFeatureAsync(uid, "pos.void"))
        {
            prefilledUsername = _currentSession.UserName;
        }

        using var authDialog = new ManagerAuthorizationForm(
            "Manager Authorization - Void Order",
            $"Order {row.OrderNumber} ({row.OrderType}) will be voided.\n\nReason: {form.Value}\n\nA manager must authorize voiding an order.",
            managerAuthService,
            "pos.void",
            prefilledUsername);

        if (authDialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        await _mediator.Send(new VoidOrderCommand(row.OrderId, form.Value!));
        await _listView.RefreshAsync();
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
