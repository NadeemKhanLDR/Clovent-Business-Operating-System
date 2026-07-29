using Clovent.Restaurant.Orders;

namespace Clovent.Restaurant.ServiceCharges;

/// <summary>Persistence contract for <see cref="ServiceCharge"/> aggregates.</summary>
public interface IServiceChargeRepository
{
    /// <summary>Retrieves a service charge by identity, or <see langword="null"/> if none exists.</summary>
    Task<ServiceCharge?> GetByIdAsync(ServiceChargeId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every service charge applied to an order.</summary>
    Task<IReadOnlyCollection<ServiceCharge>> GetByOrderIdAsync(OrderId orderId, CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created service charge.</summary>
    Task AddAsync(ServiceCharge serviceCharge, CancellationToken cancellationToken = default);
}
