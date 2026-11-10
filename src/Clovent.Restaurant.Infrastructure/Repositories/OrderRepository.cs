using Clovent.Restaurant.Customers;
using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Tables;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Restaurant.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IOrderRepository"/>.</summary>
public sealed class OrderRepository(RestaurantDbContext dbContext) : IOrderRepository
{
    /// <inheritdoc/>
    public async Task<Order?> GetByIdAsync(OrderId id, CancellationToken cancellationToken = default)
    {
        var tracked = dbContext.Orders.Local.FirstOrDefault(o => o.Id == id);
        if (tracked is not null && dbContext.Entry(tracked).State == EntityState.Unchanged)
        {
            // A screen can outlive a completion in another scope. EF identity
            // resolution otherwise returns its old Open aggregate even when the
            // SELECT sees a Completed row. Preserve actual in-flight mutations.
            await dbContext.Entry(tracked).ReloadAsync(cancellationToken).ConfigureAwait(false);
        }
        return await dbContext.Orders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Order>> GetOpenOrHeldByTableIdAsync(TableId tableId, CancellationToken cancellationToken = default) =>
        await dbContext.Orders.Where(o => o.TableId == tableId && (o.Status == OrderStatus.Open || o.Status == OrderStatus.Held)).ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<IReadOnlySet<TableId>> GetActiveTableIdsAsync(CancellationToken cancellationToken = default)
    {
        var ids = await dbContext.Orders
            .Where(o => o.TableId != null && (o.Status == OrderStatus.Open || o.Status == OrderStatus.Held))
            .Select(o => o.TableId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return ids.ToHashSet();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Order>> GetOpenAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Orders
            .Where(o => o.Status == OrderStatus.Open && dbContext.OrderLines.Any(l => l.OrderId == o.Id && !l.IsVoided && l.Quantity > 0))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Order>> GetHeldAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Orders
            .Where(o => o.Status == OrderStatus.Held && dbContext.OrderLines.Any(l => l.OrderId == o.Id && !l.IsVoided && l.Quantity > 0))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Order>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Orders.ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Order>> GetByCustomerIdAsync(CustomerId customerId, CancellationToken cancellationToken = default) =>
        await dbContext.Orders
            .Where(o => o.CustomerId == customerId)
            .OrderBy(o => o.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task AddAsync(Order order, CancellationToken cancellationToken = default) =>
        await dbContext.Orders.AddAsync(order, cancellationToken).ConfigureAwait(false);
}
