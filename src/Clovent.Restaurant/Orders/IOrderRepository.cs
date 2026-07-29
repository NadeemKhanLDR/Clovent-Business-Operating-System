using Clovent.Restaurant.Tables;

namespace Clovent.Restaurant.Orders;

/// <summary>Persistence contract for <see cref="Order"/> aggregates.</summary>
public interface IOrderRepository
{
    /// <summary>Retrieves an order by identity, or <see langword="null"/> if none exists.</summary>
    Task<Order?> GetByIdAsync(OrderId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every order currently open or held at the given table (in practice zero or one).</summary>
    Task<IReadOnlyCollection<Order>> GetOpenOrHeldByTableIdAsync(TableId tableId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every order whose status is <see cref="OrderStatus.Open"/> - the Running Orders screen's data source.</summary>
    Task<IReadOnlyCollection<Order>> GetOpenAsync(CancellationToken cancellationToken = default);

    /// <summary>Retrieves every order whose status is <see cref="OrderStatus.Held"/> - the Hold Orders screen's data source.</summary>
    Task<IReadOnlyCollection<Order>> GetHeldAsync(CancellationToken cancellationToken = default);

    /// <summary>Retrieves every order, regardless of status.</summary>
    Task<IReadOnlyCollection<Order>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created order.</summary>
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
}
