using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.PaymentMethods;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Restaurant.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IPaymentMethodRepository"/>.</summary>
public sealed class PaymentMethodRepository(RestaurantDbContext dbContext) : IPaymentMethodRepository
{
    /// <inheritdoc/>
    public async Task<PaymentMethod?> GetByIdAsync(PaymentMethodId id, CancellationToken cancellationToken = default) =>
        await dbContext.PaymentMethods.FirstOrDefaultAsync(m => m.Id == id, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<PaymentMethod>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.PaymentMethods.ToListAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task AddAsync(PaymentMethod paymentMethod, CancellationToken cancellationToken = default) =>
        await dbContext.PaymentMethods.AddAsync(paymentMethod, cancellationToken).ConfigureAwait(false);
}
