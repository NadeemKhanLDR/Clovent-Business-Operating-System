using Clovent.Domain;
using Clovent.Restaurant.KitchenTickets.Events;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;

namespace Clovent.Restaurant.KitchenTickets;

/// <summary>
/// The kitchen's view of one <see cref="Orders.Order"/> sent for
/// preparation - one ticket per order, holding a snapshot of the
/// <see cref="OrderLineId"/>s included at send-time (lines added to the
/// order afterward need a new ticket, not a mutation of this one, so the
/// kitchen never sees an order's prep list silently change underneath an
/// in-progress ticket). See <c>KitchenWorkflow.md</c>.
/// </summary>
public sealed class KitchenTicket : AggregateRoot<KitchenTicketId>
{
    private readonly HashSet<OrderLineId> _orderLineIds;

    /// <summary>The order this ticket was sent for, fixed at creation.</summary>
    public OrderId OrderId { get; }

    /// <summary>The order lines included on this ticket, fixed at creation.</summary>
    public IReadOnlyCollection<OrderLineId> OrderLineIds => _orderLineIds;

    /// <summary>The ticket's current workflow state.</summary>
    public KitchenTicketStatus Status { get; private set; }

    /// <summary>UTC instant this ticket was sent to the kitchen.</summary>
    public DateTimeOffset CreatedAtUtc { get; }

    /// <summary>UTC instant preparation began, if it has.</summary>
    public DateTimeOffset? StartedAtUtc { get; private set; }

    /// <summary>UTC instant the ticket was marked ready, if it has been.</summary>
    public DateTimeOffset? ReadyAtUtc { get; private set; }

    /// <summary>UTC instant the ticket was served, if it has been.</summary>
    public DateTimeOffset? ServedAtUtc { get; private set; }

    /// <summary>Takes every persisted field explicitly so this is the single, unambiguous constructor an EF Core Infrastructure implementation can bind to.</summary>
    private KitchenTicket(
        KitchenTicketId id,
        OrderId orderId,
        IReadOnlyCollection<OrderLineId> orderLineIds,
        KitchenTicketStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? startedAtUtc,
        DateTimeOffset? readyAtUtc,
        DateTimeOffset? servedAtUtc)
    {
        Id = id;
        OrderId = orderId;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        StartedAtUtc = startedAtUtc;
        ReadyAtUtc = readyAtUtc;
        ServedAtUtc = servedAtUtc;
        _orderLineIds = [.. orderLineIds];
    }

    /// <summary>Sends a new ticket to the kitchen for the given order's lines.</summary>
    /// <exception cref="ArgumentException"><paramref name="orderLineIds"/> is empty.</exception>
    public static KitchenTicket Create(OrderId orderId, IReadOnlyCollection<OrderLineId> orderLineIds)
    {
        if (orderLineIds.Count == 0)
            throw new ArgumentException("A kitchen ticket requires at least one order line.", nameof(orderLineIds));

        var now = DateTimeOffset.UtcNow;
        var ticket = new KitchenTicket(KitchenTicketId.New(), orderId, orderLineIds, KitchenTicketStatus.New, now, null, null, null);
        ticket.AddDomainEvent(new KitchenTicketCreated(ticket.Id, ticket.OrderId, now));
        return ticket;
    }

    /// <summary>Begins preparation.</summary>
    /// <exception cref="RestaurantDomainException">The ticket is not <see cref="KitchenTicketStatus.New"/>.</exception>
    public void Start()
    {
        if (Status != KitchenTicketStatus.New)
            throw RestaurantDomainException.KitchenTicketNotNew(Id);

        Status = KitchenTicketStatus.InProgress;
        StartedAtUtc = DateTimeOffset.UtcNow;
        AddDomainEvent(new KitchenTicketStarted(Id, StartedAtUtc.Value));
    }

    /// <summary>Marks the ticket ready to serve.</summary>
    /// <exception cref="RestaurantDomainException">The ticket is not <see cref="KitchenTicketStatus.InProgress"/>.</exception>
    public void MarkReady()
    {
        if (Status != KitchenTicketStatus.InProgress)
            throw RestaurantDomainException.KitchenTicketNotInProgress(Id);

        Status = KitchenTicketStatus.Ready;
        ReadyAtUtc = DateTimeOffset.UtcNow;
        AddDomainEvent(new KitchenTicketMarkedReady(Id, ReadyAtUtc.Value));
    }

    /// <summary>Marks the ticket served.</summary>
    /// <exception cref="RestaurantDomainException">The ticket is not <see cref="KitchenTicketStatus.Ready"/>.</exception>
    public void Serve()
    {
        if (Status != KitchenTicketStatus.Ready)
            throw RestaurantDomainException.KitchenTicketNotReady(Id);

        Status = KitchenTicketStatus.Served;
        ServedAtUtc = DateTimeOffset.UtcNow;
        AddDomainEvent(new KitchenTicketServed(Id, ServedAtUtc.Value));
    }

    /// <summary>Cancels the ticket before it is served.</summary>
    /// <exception cref="RestaurantDomainException">The ticket is already <see cref="KitchenTicketStatus.Served"/> or <see cref="KitchenTicketStatus.Cancelled"/>.</exception>
    public void Cancel()
    {
        if (Status is KitchenTicketStatus.Served or KitchenTicketStatus.Cancelled)
            throw RestaurantDomainException.KitchenTicketCannotBeCancelled(Id, Status);

        Status = KitchenTicketStatus.Cancelled;
        AddDomainEvent(new KitchenTicketCancelled(Id, DateTimeOffset.UtcNow));
    }
}
