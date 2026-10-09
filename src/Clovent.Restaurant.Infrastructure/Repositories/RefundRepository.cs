using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Refunds;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Restaurant.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IRefundRepository"/>.</summary>
public sealed class RefundRepository(RestaurantDbContext dbContext) : IRefundRepository
{
    /// <inheritdoc/>
    public Task<Refund?> GetByIdAsync(RefundId id, CancellationToken cancellationToken = default) =>
        dbContext.Refunds
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    /// <inheritdoc/>
    public Task<Refund?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default) =>
        dbContext.Refunds
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.IdempotencyKey == idempotencyKey, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Refund>> GetByOrderIdAsync(OrderId orderId, CancellationToken cancellationToken = default) =>
        await dbContext.Refunds
            .Include(r => r.Lines)
            .Where(r => r.OrderId == orderId)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Refund>> GetByDateRangeAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default) =>
        await dbContext.Refunds
            .Include(r => r.Lines)
            .Where(r => r.RefundedAtUtc >= fromUtc && r.RefundedAtUtc <= toUtc)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Refund>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Refunds
            .Include(r => r.Lines)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(Refund refund, CancellationToken cancellationToken = default) =>
        await dbContext.Refunds.AddAsync(refund, cancellationToken);
}
