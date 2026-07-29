using Clovent.Restaurant.Orders;

namespace Clovent.Restaurant.Discounts;

/// <summary>Persistence contract for <see cref="Discount"/> aggregates.</summary>
public interface IDiscountRepository
{
    /// <summary>Retrieves a discount by identity, or <see langword="null"/> if none exists.</summary>
    Task<Discount?> GetByIdAsync(DiscountId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every discount applied to an order.</summary>
    Task<IReadOnlyCollection<Discount>> GetByOrderIdAsync(OrderId orderId, CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created discount.</summary>
    Task AddAsync(Discount discount, CancellationToken cancellationToken = default);
}
