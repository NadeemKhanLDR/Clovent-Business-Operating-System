using Clovent.Restaurant.Application;

namespace Clovent.Restaurant.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IUnitOfWork"/>, wrapping <see cref="RestaurantDbContext"/>'s <c>SaveChangesAsync</c>. Mirrors <c>Clovent.Catalog.Infrastructure.Persistence.UnitOfWork</c>.</summary>
public sealed class UnitOfWork(RestaurantDbContext dbContext) : IUnitOfWork
{
    /// <inheritdoc/>
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SaveChangesAsync(cancellationToken);
}
