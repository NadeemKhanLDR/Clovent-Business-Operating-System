using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Orders;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Restaurant.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IOrderNumberSequenceRepository"/>.</summary>
public sealed class OrderNumberSequenceRepository(RestaurantDbContext dbContext) : IOrderNumberSequenceRepository
{
    /// <inheritdoc/>
    public Task<OrderNumberSequence?> GetSingletonAsync(CancellationToken cancellationToken = default) =>
        dbContext.OrderNumberSequences.FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(OrderNumberSequence sequence, CancellationToken cancellationToken = default) =>
        await dbContext.OrderNumberSequences.AddAsync(sequence, cancellationToken);
}
