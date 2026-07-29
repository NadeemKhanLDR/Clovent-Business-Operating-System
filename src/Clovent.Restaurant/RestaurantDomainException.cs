using Clovent.Domain;
using Clovent.Restaurant.DiningAreas;
using Clovent.Restaurant.KitchenTickets;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.Payments;
using Clovent.Restaurant.Tables;

namespace Clovent.Restaurant;

/// <summary>
/// Raised when a Restaurant POS aggregate operation would violate one of
/// its invariants - mirrors <c>Clovent.Catalog.CatalogDomainException</c>
/// exactly: one sealed type, one static factory method per rule.
/// </summary>
public sealed class RestaurantDomainException : DomainException
{
    private RestaurantDomainException(string message) : base(message)
    {
    }

    /// <summary>A dining area Activate() was attempted while already active.</summary>
    public static RestaurantDomainException DiningAreaAlreadyActive(DiningAreaId diningAreaId) =>
        new($"Dining area '{diningAreaId}' is already active.");

    /// <summary>A dining area Deactivate() was attempted while not active.</summary>
    public static RestaurantDomainException DiningAreaNotActive(DiningAreaId diningAreaId) =>
        new($"Dining area '{diningAreaId}' is not active.");

    /// <summary>A table Occupy() was attempted while it could not be seated.</summary>
    public static RestaurantDomainException TableCannotBeOccupied(TableId tableId, TableOccupancyStatus current) =>
        new($"Table '{tableId}' cannot be occupied while it is {current}.");

    /// <summary>A table Vacate() was attempted while out of service.</summary>
    public static RestaurantDomainException TableCannotBeVacated(TableId tableId) =>
        new($"Table '{tableId}' cannot be vacated while out of service.");

    /// <summary>A table Reserve()/SetOutOfService() was attempted while not available.</summary>
    public static RestaurantDomainException TableNotAvailable(TableId tableId) =>
        new($"Table '{tableId}' is not available.");

    /// <summary>A table ReturnToService() was attempted while not out of service.</summary>
    public static RestaurantDomainException TableNotOutOfService(TableId tableId) =>
        new($"Table '{tableId}' is not out of service.");

    /// <summary>A table Activate() was attempted while already active.</summary>
    public static RestaurantDomainException TableAlreadyActive(TableId tableId) =>
        new($"Table '{tableId}' is already active.");

    /// <summary>A table Deactivate() was attempted while not active.</summary>
    public static RestaurantDomainException TableNotActive(TableId tableId) =>
        new($"Table '{tableId}' is not active.");

    /// <summary>A dine-in Order.Create() was attempted with no table.</summary>
    public static RestaurantDomainException DineInOrderRequiresTable() =>
        new("A dine-in order requires a table.");

    /// <summary>A take-away Order.Create() was attempted with a table.</summary>
    public static RestaurantDomainException TakeAwayOrderMustNotHaveTable() =>
        new("A take-away order cannot have a table.");

    /// <summary>An Order.AssignTable() was attempted on a take-away order.</summary>
    public static RestaurantDomainException TakeAwayOrderCannotBeAssignedTable(OrderId orderId) =>
        new($"Order '{orderId}' is take-away and cannot be assigned a table.");

    /// <summary>An operation requiring <see cref="OrderStatus.Open"/> was attempted while the order was not.</summary>
    public static RestaurantDomainException OrderNotOpen(OrderId orderId, OrderStatus status) =>
        new($"Order '{orderId}' is {status}, not Open.");

    /// <summary>Order.Resume() was attempted while not Held.</summary>
    public static RestaurantDomainException OrderNotHeld(OrderId orderId) =>
        new($"Order '{orderId}' is not held.");

    /// <summary>Order.Void() was attempted while already Voided or Cancelled.</summary>
    public static RestaurantDomainException OrderCannotBeVoided(OrderId orderId, OrderStatus status) =>
        new($"Order '{orderId}' cannot be voided while {status}.");

    /// <summary>Order.Cancel() was attempted while not Open/Held.</summary>
    public static RestaurantDomainException OrderCannotBeCancelled(OrderId orderId, OrderStatus status) =>
        new($"Order '{orderId}' cannot be cancelled while {status}.");

    /// <summary>Order.Cancel() was attempted while a payment had already been recorded.</summary>
    public static RestaurantDomainException OrderHasPayments(OrderId orderId) =>
        new($"Order '{orderId}' cannot be cancelled because it already has a recorded payment.");

    /// <summary>Order.Reopen() was attempted while not Voided/Cancelled.</summary>
    public static RestaurantDomainException OrderCannotBeReopened(OrderId orderId, OrderStatus status) =>
        new($"Order '{orderId}' cannot be reopened while {status}.");

    /// <summary>Order.AddOrderLine() was attempted with a line already on the order.</summary>
    public static RestaurantDomainException OrderLineAlreadyOnOrder(OrderId orderId, OrderLineId orderLineId) =>
        new($"Order line '{orderLineId}' already belongs to order '{orderId}'.");

    /// <summary>Order.RemoveOrderLine() was attempted with a line not on the order.</summary>
    public static RestaurantDomainException OrderLineNotOnOrder(OrderId orderId, OrderLineId orderLineId) =>
        new($"Order line '{orderLineId}' does not belong to order '{orderId}'.");

    /// <summary>An OrderLine.Void() was attempted while already voided.</summary>
    public static RestaurantDomainException OrderLineAlreadyVoided(OrderLineId orderLineId) =>
        new($"Order line '{orderLineId}' is already voided.");

    /// <summary>An OrderLine.Unvoid() was attempted while not voided.</summary>
    public static RestaurantDomainException OrderLineNotVoided(OrderLineId orderLineId) =>
        new($"Order line '{orderLineId}' is not voided.");

    /// <summary>A PaymentMethod.Activate() was attempted while already active.</summary>
    public static RestaurantDomainException PaymentMethodAlreadyActive(PaymentMethodId paymentMethodId) =>
        new($"Payment method '{paymentMethodId}' is already active.");

    /// <summary>A PaymentMethod.Deactivate() was attempted while not active.</summary>
    public static RestaurantDomainException PaymentMethodNotActive(PaymentMethodId paymentMethodId) =>
        new($"Payment method '{paymentMethodId}' is not active.");

    /// <summary>A Payment.Void() was attempted while already voided.</summary>
    public static RestaurantDomainException PaymentAlreadyVoided(PaymentId paymentId) =>
        new($"Payment '{paymentId}' is already voided.");

    /// <summary>A KitchenTicket.Start() was attempted while not New.</summary>
    public static RestaurantDomainException KitchenTicketNotNew(KitchenTicketId kitchenTicketId) =>
        new($"Kitchen ticket '{kitchenTicketId}' is not new.");

    /// <summary>A KitchenTicket.MarkReady() was attempted while not InProgress.</summary>
    public static RestaurantDomainException KitchenTicketNotInProgress(KitchenTicketId kitchenTicketId) =>
        new($"Kitchen ticket '{kitchenTicketId}' is not in progress.");

    /// <summary>A KitchenTicket.Serve() was attempted while not Ready.</summary>
    public static RestaurantDomainException KitchenTicketNotReady(KitchenTicketId kitchenTicketId) =>
        new($"Kitchen ticket '{kitchenTicketId}' is not ready.");

    /// <summary>A KitchenTicket.Cancel() was attempted while already Served or Cancelled.</summary>
    public static RestaurantDomainException KitchenTicketCannotBeCancelled(KitchenTicketId kitchenTicketId, KitchenTicketStatus status) =>
        new($"Kitchen ticket '{kitchenTicketId}' cannot be cancelled while {status}.");

    /// <summary>A CompleteOrderCommand was attempted while the order's balance was still positive - raised by the Application layer, which is the only place able to compute a balance across the order's lines/discounts/service charges/payments (see <c>Order</c>'s doc comment).</summary>
    public static RestaurantDomainException OrderNotFullyPaid(OrderId orderId, decimal balance) =>
        new($"Order '{orderId}' still owes {balance:0.00} and cannot be completed.");

    /// <summary>A MergeTablesCommand/SplitOrderCommand was attempted with a source table/order that has no open or held order to move lines from.</summary>
    public static RestaurantDomainException TableHasNoOpenOrder(TableId tableId) =>
        new($"Table '{tableId}' has no open or held order.");

    /// <summary>A MergeTablesCommand was attempted merging a table into itself.</summary>
    public static RestaurantDomainException CannotMergeTableIntoItself(TableId tableId) =>
        new($"Table '{tableId}' cannot be merged into itself.");
}
