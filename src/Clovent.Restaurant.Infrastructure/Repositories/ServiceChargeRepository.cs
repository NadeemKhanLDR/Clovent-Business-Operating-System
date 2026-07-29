using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.ServiceCharges;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Restaurant.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IServiceChargeRepository"/>.</summary>
public sealed class ServiceChargeRepository(RestaurantDbContext dbContext) : IServiceChargeRepository
{
    /// <inheritdoc/>
    public Task<ServiceCharge?> GetByIdAsync(ServiceChargeId id, CancellationToken cancellationToken = default) =>
        dbContext.ServiceCharges.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<ServiceCharge>> GetByOrderIdAsync(OrderId orderId, CancellationToken cancellationToken = default) =>
        await dbContext.ServiceCharges.Where(s => s.OrderId == orderId).ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(ServiceCharge serviceCharge, CancellationToken cancellationToken = default) =>
        await dbContext.ServiceCharges.AddAsync(serviceCharge, cancellationToken);
}
