using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Restaurant.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IOrderLineRepository"/>.</summary>
public sealed class OrderLineRepository(RestaurantDbContext dbContext) : IOrderLineRepository
{
    /// <inheritdoc/>
    public Task<OrderLine?> GetByIdAsync(OrderLineId id, CancellationToken cancellationToken = default) =>
        dbContext.OrderLines.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<OrderLine>> GetByOrderIdAsync(OrderId orderId, CancellationToken cancellationToken = default) =>
        await dbContext.OrderLines.Where(l => l.OrderId == orderId).ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(OrderLine orderLine, CancellationToken cancellationToken = default) =>
        await dbContext.OrderLines.AddAsync(orderLine, cancellationToken);
}
