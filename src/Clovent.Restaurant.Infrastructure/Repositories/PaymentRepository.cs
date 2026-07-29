using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Payments;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Restaurant.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IPaymentRepository"/>.</summary>
public sealed class PaymentRepository(RestaurantDbContext dbContext) : IPaymentRepository
{
    /// <inheritdoc/>
    public Task<Payment?> GetByIdAsync(PaymentId id, CancellationToken cancellationToken = default) =>
        dbContext.Payments.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Payment>> GetByOrderIdAsync(OrderId orderId, CancellationToken cancellationToken = default) =>
        await dbContext.Payments.Where(p => p.OrderId == orderId).ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default) =>
        await dbContext.Payments.AddAsync(payment, cancellationToken);
}
