using Clovent.Platform.Bootstrap;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Catalog.Infrastructure.Persistence;

/// <summary>Applies pending EF Core migrations for <see cref="CatalogDbContext"/> at startup.</summary>
public sealed class CatalogPersistenceInitializer(CatalogDbContext dbContext) : IPersistenceInitializer
{
    /// <inheritdoc/>
    public Task InitializeAsync(CancellationToken cancellationToken = default) =>
        dbContext.Database.MigrateAsync(cancellationToken);
}
