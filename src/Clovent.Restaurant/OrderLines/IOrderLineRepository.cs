using Clovent.Restaurant.Orders;

namespace Clovent.Restaurant.OrderLines;

/// <summary>Persistence contract for <see cref="OrderLine"/> aggregates.</summary>
public interface IOrderLineRepository
{
    /// <summary>Retrieves an order line by identity, or <see langword="null"/> if none exists.</summary>
    Task<OrderLine?> GetByIdAsync(OrderLineId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every line currently belonging to an order.</summary>
    Task<IReadOnlyCollection<OrderLine>> GetByOrderIdAsync(OrderId orderId, CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created order line.</summary>
    Task AddAsync(OrderLine orderLine, CancellationToken cancellationToken = default);
}
