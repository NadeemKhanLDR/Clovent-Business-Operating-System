using Clovent.Restaurant.Orders;

namespace Clovent.Restaurant.Refunds;

/// <summary>Persistence contract for <see cref="Refund"/> aggregate roots.</summary>
public interface IRefundRepository
{
    /// <summary>Retrieves a refund by its unique ID, including lines.</summary>
    Task<Refund?> GetByIdAsync(RefundId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a refund by its idempotency key for duplicate prevention.</summary>
    Task<Refund?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    /// <summary>Retrieves all refunds posted against an original order.</summary>
    Task<IReadOnlyList<Refund>> GetByOrderIdAsync(OrderId orderId, CancellationToken cancellationToken = default);

    /// <summary>Retrieves all refunds within a date range.</summary>
    Task<IReadOnlyList<Refund>> GetByDateRangeAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default);

    /// <summary>Retrieves all refunds.</summary>
    Task<IReadOnlyList<Refund>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a new refund aggregate and its lines.</summary>
    Task AddAsync(Refund refund, CancellationToken cancellationToken = default);
}
