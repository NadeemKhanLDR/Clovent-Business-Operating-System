using Clovent.Domain;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Discounts;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders.Events;
using Clovent.Restaurant.Orders.ValueObjects;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.ServiceCharges;
using Clovent.Restaurant.Tables;

namespace Clovent.Restaurant.Orders;

/// <summary>
/// The central Restaurant POS aggregate - one dine-in or take-away sale in
/// progress. Deliberately holds no computed total: <see cref="OrderLineIds"/>/<see cref="DiscountIds"/>/<see cref="ServiceChargeIds"/>/<see cref="PaymentIds"/>
/// each reference their own aggregate by id only (the same
/// "list of child ids" pattern <c>Clovent.Identity.Organizations.Organization.CompanyIds</c>
/// already established), so the Application layer computes the running
/// total/tax/balance by loading those aggregates rather than this one
/// aggregate trying to duplicate arithmetic that belongs to its children -
/// see <c>OrderLifecycle.md</c>.
/// </summary>
public sealed class Order : AggregateRoot<OrderId>
{
    private readonly HashSet<OrderLineId> _orderLineIds;
    private readonly HashSet<DiscountId> _discountIds;
    private readonly HashSet<ServiceChargeId> _serviceChargeIds;
    private readonly HashSet<PaymentId> _paymentIds;

    /// <summary>The order's human-facing display number.</summary>
    public OrderNumber OrderNumber { get; }

    /// <summary>
    /// The order's per-day, per-warehouse sequential number (e.g. "#7 today
    /// at this warehouse"), assigned once by <see cref="AssignDailySalesNumber"/>
    /// when the order completes - <see langword="null"/> until then, since an
    /// order in progress has no place in the day's completed-sale sequence
    /// yet. Distinct from <see cref="OrderNumber"/>, which is globally unique
    /// and assigned at creation.
    /// </summary>
    public int? DailySalesNumber { get; private set; }

    /// <summary>Dine-in or take-away, fixed at creation.</summary>
    public OrderType OrderType { get; }

    /// <summary>The order's current workflow state.</summary>
    public OrderStatus Status { get; private set; }

    /// <summary>The table this order is seated at - always set for <see cref="OrderType.DineIn"/>, always <see langword="null"/> for <see cref="OrderType.TakeAway"/>.</summary>
    public TableId? TableId { get; private set; }

    /// <summary>The warehouse stock is drawn from when this order completes, fixed at creation.</summary>
    public WarehouseId WarehouseId { get; }

    /// <summary>Free-text internal notes (kitchen/staff-facing), if any.</summary>
    public string? Notes { get; private set; }

    /// <summary>Free-text customer-facing notes (e.g. printed on a receipt), if any.</summary>
    public string? CustomerNotes { get; private set; }

    /// <summary>The order lines currently on this order.</summary>
    public IReadOnlyCollection<OrderLineId> OrderLineIds => _orderLineIds;

    /// <summary>The discounts currently applied to this order.</summary>
    public IReadOnlyCollection<DiscountId> DiscountIds => _discountIds;

    /// <summary>The service charges currently applied to this order.</summary>
    public IReadOnlyCollection<ServiceChargeId> ServiceChargeIds => _serviceChargeIds;

    /// <summary>The payments recorded against this order.</summary>
    public IReadOnlyCollection<PaymentId> PaymentIds => _paymentIds;

    /// <summary>UTC instant this order was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>UTC instant this order was last changed.</summary>
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    /// <summary>Takes every persisted field explicitly so this is the single, unambiguous constructor an EF Core Infrastructure implementation can bind to.</summary>
    private Order(
        OrderId id,
        OrderNumber orderNumber,
        int? dailySalesNumber,
        OrderType orderType,
        OrderStatus status,
        TableId? tableId,
        WarehouseId warehouseId,
        string? notes,
        string? customerNotes,
        IReadOnlyCollection<OrderLineId> orderLineIds,
        IReadOnlyCollection<DiscountId> discountIds,
        IReadOnlyCollection<ServiceChargeId> serviceChargeIds,
        IReadOnlyCollection<PaymentId> paymentIds,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        OrderNumber = orderNumber;
        DailySalesNumber = dailySalesNumber;
        OrderType = orderType;
        Status = status;
        TableId = tableId;
        WarehouseId = warehouseId;
        Notes = notes;
        CustomerNotes = customerNotes;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        _orderLineIds = [.. orderLineIds];
        _discountIds = [.. discountIds];
        _serviceChargeIds = [.. serviceChargeIds];
        _paymentIds = [.. paymentIds];
    }

    /// <summary>Creates a new, open order.</summary>
    /// <exception cref="RestaurantDomainException"><paramref name="orderType"/> is <see cref="OrderType.DineIn"/> with no <paramref name="tableId"/>, or <see cref="OrderType.TakeAway"/> with one.</exception>
    public static Order Create(OrderType orderType, WarehouseId warehouseId, TableId? tableId = null)
    {
        RequireConsistentTable(orderType, tableId);

        var now = DateTimeOffset.UtcNow;
        var order = new Order(
            OrderId.New(), OrderNumber.Generate(now), null, orderType, OrderStatus.Open, tableId, warehouseId,
            null, null, [], [], [], [], now, now);
        order.AddDomainEvent(new OrderCreated(order.Id, order.OrderNumber, order.OrderType, order.WarehouseId, order.TableId, now));
        return order;
    }

    /// <summary>
    /// Assigns this order's Daily Sales Number, once - called by
    /// <c>CompleteOrderCommandHandler</c> right before <see cref="Complete"/>,
    /// so the number is only ever given to a sale that is truly finalized.
    /// </summary>
    /// <exception cref="RestaurantDomainException">A Daily Sales Number has already been assigned.</exception>
    public void AssignDailySalesNumber(int number)
    {
        if (DailySalesNumber is not null)
            throw RestaurantDomainException.DailySalesNumberAlreadyAssigned(Id);

        DailySalesNumber = number;
        Touch();
        AddDomainEvent(new OrderDailySalesNumberAssigned(Id, number, DateTimeOffset.UtcNow));
    }

    /// <summary>Adds an order line. Only while <see cref="OrderStatus.Open"/>.</summary>
    /// <exception cref="RestaurantDomainException">The order is not <see cref="OrderStatus.Open"/>, or the line already belongs to this order.</exception>
    public void AddOrderLine(OrderLineId orderLineId)
    {
        RequireOpen();
        if (!_orderLineIds.Add(orderLineId))
            throw RestaurantDomainException.OrderLineAlreadyOnOrder(Id, orderLineId);

        Touch();
        AddDomainEvent(new OrderLineAdded(Id, orderLineId, DateTimeOffset.UtcNow));
    }

    /// <summary>Removes an order line. Only while <see cref="OrderStatus.Open"/>.</summary>
    /// <exception cref="RestaurantDomainException">The order is not <see cref="OrderStatus.Open"/>, or the line does not belong to this order.</exception>
    public void RemoveOrderLine(OrderLineId orderLineId)
    {
        RequireOpen();
        if (!_orderLineIds.Remove(orderLineId))
            throw RestaurantDomainException.OrderLineNotOnOrder(Id, orderLineId);

        Touch();
        AddDomainEvent(new OrderLineRemoved(Id, orderLineId, DateTimeOffset.UtcNow));
    }

    /// <summary>Applies a discount. Only while <see cref="OrderStatus.Open"/>.</summary>
    public void ApplyDiscount(DiscountId discountId)
    {
        RequireOpen();
        if (!_discountIds.Add(discountId))
            return;

        Touch();
        AddDomainEvent(new OrderDiscountApplied(Id, discountId, DateTimeOffset.UtcNow));
    }

    /// <summary>Removes a discount. Only while <see cref="OrderStatus.Open"/>.</summary>
    public void RemoveDiscount(DiscountId discountId)
    {
        RequireOpen();
        if (!_discountIds.Remove(discountId))
            return;

        Touch();
        AddDomainEvent(new OrderDiscountRemoved(Id, discountId, DateTimeOffset.UtcNow));
    }

    /// <summary>Applies a service charge. Only while <see cref="OrderStatus.Open"/>.</summary>
    public void ApplyServiceCharge(ServiceChargeId serviceChargeId)
    {
        RequireOpen();
        if (!_serviceChargeIds.Add(serviceChargeId))
            return;

        Touch();
        AddDomainEvent(new OrderServiceChargeApplied(Id, serviceChargeId, DateTimeOffset.UtcNow));
    }

    /// <summary>Removes a service charge. Only while <see cref="OrderStatus.Open"/>.</summary>
    public void RemoveServiceCharge(ServiceChargeId serviceChargeId)
    {
        RequireOpen();
        if (!_serviceChargeIds.Remove(serviceChargeId))
            return;

        Touch();
        AddDomainEvent(new OrderServiceChargeRemoved(Id, serviceChargeId, DateTimeOffset.UtcNow));
    }

    /// <summary>Records a payment. Allowed while <see cref="OrderStatus.Open"/> - partial and multiple payments both accumulate here before <see cref="Complete"/>.</summary>
    public void RecordPayment(PaymentId paymentId)
    {
        RequireOpen();
        if (!_paymentIds.Add(paymentId))
            return;

        Touch();
        AddDomainEvent(new OrderPaymentRecorded(Id, paymentId, DateTimeOffset.UtcNow));
    }

    /// <summary>Sets the order's internal notes. A no-op (no event raised) if unchanged.</summary>
    public void SetNotes(string? notes)
    {
        if (Notes == notes) return;

        Notes = notes;
        Touch();
        AddDomainEvent(new OrderNotesChanged(Id, notes, DateTimeOffset.UtcNow));
    }

    /// <summary>Sets the order's customer-facing notes. A no-op (no event raised) if unchanged.</summary>
    public void SetCustomerNotes(string? customerNotes)
    {
        if (CustomerNotes == customerNotes) return;

        CustomerNotes = customerNotes;
        Touch();
        AddDomainEvent(new OrderCustomerNotesChanged(Id, customerNotes, DateTimeOffset.UtcNow));
    }

    /// <summary>Assigns (or reassigns, for a table transfer) the table this order is seated at.</summary>
    /// <exception cref="RestaurantDomainException">This is a <see cref="OrderType.TakeAway"/> order.</exception>
    public void AssignTable(TableId tableId)
    {
        if (OrderType != OrderType.DineIn)
            throw RestaurantDomainException.TakeAwayOrderCannotBeAssignedTable(Id);

        if (TableId == tableId) return;

        TableId = tableId;
        Touch();
        AddDomainEvent(new OrderTableAssigned(Id, tableId, DateTimeOffset.UtcNow));
    }

    /// <summary>Holds the order.</summary>
    /// <exception cref="RestaurantDomainException">The order is not <see cref="OrderStatus.Open"/>.</exception>
    public void Hold()
    {
        RequireOpen();
        Status = OrderStatus.Held;
        Touch();
        AddDomainEvent(new OrderHeld(Id, DateTimeOffset.UtcNow));
    }

    /// <summary>Resumes a held order.</summary>
    /// <exception cref="RestaurantDomainException">The order is not <see cref="OrderStatus.Held"/>.</exception>
    public void Resume()
    {
        if (Status != OrderStatus.Held)
            throw RestaurantDomainException.OrderNotHeld(Id);

        Status = OrderStatus.Open;
        Touch();
        AddDomainEvent(new OrderResumed(Id, DateTimeOffset.UtcNow));
    }

    /// <summary>Voids the order - allowed from <see cref="OrderStatus.Open"/>, <see cref="OrderStatus.Held"/>, or <see cref="OrderStatus.Completed"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="reason"/> is empty.</exception>
    /// <exception cref="RestaurantDomainException">The order is already <see cref="OrderStatus.Voided"/> or <see cref="OrderStatus.Cancelled"/>.</exception>
    public void Void(string reason)
    {
        reason = RequireReason(reason);
        if (Status is OrderStatus.Voided or OrderStatus.Cancelled)
            throw RestaurantDomainException.OrderCannotBeVoided(Id, Status);

        Status = OrderStatus.Voided;
        Touch();
        AddDomainEvent(new OrderVoided(Id, reason, DateTimeOffset.UtcNow));
    }

    /// <summary>Cancels the order before any payment. Allowed from <see cref="OrderStatus.Open"/> or <see cref="OrderStatus.Held"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="reason"/> is empty.</exception>
    /// <exception cref="RestaurantDomainException">The order is not <see cref="OrderStatus.Open"/>/<see cref="OrderStatus.Held"/>, or already has a recorded payment.</exception>
    public void Cancel(string reason)
    {
        reason = RequireReason(reason);
        if (Status is not (OrderStatus.Open or OrderStatus.Held))
            throw RestaurantDomainException.OrderCannotBeCancelled(Id, Status);

        if (_paymentIds.Count > 0)
            throw RestaurantDomainException.OrderHasPayments(Id);

        Status = OrderStatus.Cancelled;
        Touch();
        AddDomainEvent(new OrderCancelled(Id, reason, DateTimeOffset.UtcNow));
    }

    /// <summary>Reopens a voided or cancelled order back to <see cref="OrderStatus.Open"/>.</summary>
    /// <exception cref="RestaurantDomainException">The order is not <see cref="OrderStatus.Voided"/> or <see cref="OrderStatus.Cancelled"/>.</exception>
    public void Reopen()
    {
        if (Status is not (OrderStatus.Voided or OrderStatus.Cancelled))
            throw RestaurantDomainException.OrderCannotBeReopened(Id, Status);

        Status = OrderStatus.Open;
        Touch();
        AddDomainEvent(new OrderReopened(Id, DateTimeOffset.UtcNow));
    }

    /// <summary>Completes the order (fully paid and closed). The caller is responsible for having verified the balance is zero first - see <c>OrderLifecycle.md</c>.</summary>
    /// <exception cref="RestaurantDomainException">The order is not <see cref="OrderStatus.Open"/>.</exception>
    public void Complete()
    {
        RequireOpen();
        Status = OrderStatus.Completed;
        Touch();
        AddDomainEvent(new OrderCompleted(Id, DateTimeOffset.UtcNow));
    }

    private void RequireOpen()
    {
        if (Status != OrderStatus.Open)
            throw RestaurantDomainException.OrderNotOpen(Id, Status);
    }

    private void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;

    private static void RequireConsistentTable(OrderType orderType, TableId? tableId)
    {
        if (orderType == OrderType.DineIn && tableId is null)
            throw RestaurantDomainException.DineInOrderRequiresTable();

        if (orderType == OrderType.TakeAway && tableId is not null)
            throw RestaurantDomainException.TakeAwayOrderMustNotHaveTable();
    }

    private static string RequireReason(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("A reason is required.", nameof(value));

        return value.Trim();
    }
}
