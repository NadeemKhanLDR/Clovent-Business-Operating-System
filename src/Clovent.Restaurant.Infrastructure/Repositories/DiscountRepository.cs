using Clovent.Restaurant.Discounts;
using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Orders;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Restaurant.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IDiscountRepository"/>.</summary>
public sealed class DiscountRepository(RestaurantDbContext dbContext) : IDiscountRepository
{
    /// <inheritdoc/>
    public Task<Discount?> GetByIdAsync(DiscountId id, CancellationToken cancellationToken = default) =>
        dbContext.Discounts.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Discount>> GetByOrderIdAsync(OrderId orderId, CancellationToken cancellationToken = default) =>
        await dbContext.Discounts.Where(d => d.OrderId == orderId).ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(Discount discount, CancellationToken cancellationToken = default) =>
        await dbContext.Discounts.AddAsync(discount, cancellationToken);
}
