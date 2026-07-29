using Clovent.Platform.Bootstrap;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Restaurant.Infrastructure.Persistence;

/// <summary>Applies pending EF Core migrations for <see cref="RestaurantDbContext"/> at startup.</summary>
public sealed class RestaurantPersistenceInitializer(RestaurantDbContext dbContext) : IPersistenceInitializer
{
    /// <inheritdoc/>
    public Task InitializeAsync(CancellationToken cancellationToken = default) =>
        dbContext.Database.MigrateAsync(cancellationToken);
}
