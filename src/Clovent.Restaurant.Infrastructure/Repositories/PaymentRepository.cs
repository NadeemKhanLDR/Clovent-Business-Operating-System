using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Payments;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Restaurant.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IPaymentRepository"/>.</summary>
public sealed class PaymentRepository(RestaurantDbContext dbContext) : IPaymentRepository
{
    /// <inheritdoc/>
    public async Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken cancellationToken = default)
    {
        var tracked = dbContext.Payments.Local.FirstOrDefault(p => p.Id == id);
        if (tracked is not null && dbContext.Entry(tracked).State == EntityState.Unchanged)
            await dbContext.Entry(tracked).ReloadAsync(cancellationToken).ConfigureAwait(false);
        return await dbContext.Payments.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Payment>> GetByOrderIdAsync(OrderId orderId, CancellationToken cancellationToken = default) =>
        await dbContext.Payments.AsNoTracking().Where(p => p.OrderId == orderId).ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Payment>> GetByShiftIdAsync(Clovent.Restaurant.Shifts.ShiftId shiftId, CancellationToken cancellationToken = default) =>
        await dbContext.Payments.AsNoTracking().Where(p => p.ShiftId == shiftId).ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Payment>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Payments.AsNoTracking().ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public Task<Payment?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default) =>
        dbContext.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey, cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default) =>
        await dbContext.Payments.AddAsync(payment, cancellationToken);
}
