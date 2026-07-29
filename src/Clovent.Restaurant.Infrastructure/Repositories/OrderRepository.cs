using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Tables;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Restaurant.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IOrderRepository"/>.</summary>
public sealed class OrderRepository(RestaurantDbContext dbContext) : IOrderRepository
{
    /// <inheritdoc/>
    public Task<Order?> GetByIdAsync(OrderId id, CancellationToken cancellationToken = default) =>
        dbContext.Orders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Order>> GetOpenOrHeldByTableIdAsync(TableId tableId, CancellationToken cancellationToken = default) =>
        await dbContext.Orders.Where(o => o.TableId == tableId && (o.Status == OrderStatus.Open || o.Status == OrderStatus.Held)).ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Order>> GetOpenAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Orders.Where(o => o.Status == OrderStatus.Open).ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Order>> GetHeldAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Orders.Where(o => o.Status == OrderStatus.Held).ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Order>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Orders.ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(Order order, CancellationToken cancellationToken = default) =>
        await dbContext.Orders.AddAsync(order, cancellationToken);
}
