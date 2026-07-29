using Clovent.Catalog.Application;

namespace Clovent.Catalog.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IUnitOfWork"/>, wrapping <see cref="CatalogDbContext"/>'s <c>SaveChangesAsync</c>. Mirrors <c>Clovent.MasterData.Infrastructure.Persistence.UnitOfWork</c>.</summary>
public sealed class UnitOfWork(CatalogDbContext dbContext) : IUnitOfWork
{
    /// <inheritdoc/>
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SaveChangesAsync(cancellationToken);
}
