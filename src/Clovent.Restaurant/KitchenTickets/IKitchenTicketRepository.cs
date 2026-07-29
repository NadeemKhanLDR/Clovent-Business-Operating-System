using Clovent.Restaurant.Orders;

namespace Clovent.Restaurant.KitchenTickets;

/// <summary>Persistence contract for <see cref="KitchenTicket"/> aggregates.</summary>
public interface IKitchenTicketRepository
{
    /// <summary>Retrieves a kitchen ticket by identity, or <see langword="null"/> if none exists.</summary>
    Task<KitchenTicket?> GetByIdAsync(KitchenTicketId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every ticket sent for an order.</summary>
    Task<IReadOnlyCollection<KitchenTicket>> GetByOrderIdAsync(OrderId orderId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every ticket not yet <see cref="KitchenTicketStatus.Served"/> or <see cref="KitchenTicketStatus.Cancelled"/> - the Kitchen Ticket Viewer's data source.</summary>
    Task<IReadOnlyCollection<KitchenTicket>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created kitchen ticket.</summary>
    Task AddAsync(KitchenTicket kitchenTicket, CancellationToken cancellationToken = default);
}
