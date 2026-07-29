using Clovent.Platform.Bootstrap;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Identity.Infrastructure.Persistence;

/// <summary>Applies pending EF Core migrations for <see cref="IdentityDbContext"/> at startup.</summary>
public sealed class IdentityPersistenceInitializer(IdentityDbContext dbContext) : IPersistenceInitializer
{
    /// <inheritdoc/>
    public Task InitializeAsync(CancellationToken cancellationToken = default) =>
        dbContext.Database.MigrateAsync(cancellationToken);
}
