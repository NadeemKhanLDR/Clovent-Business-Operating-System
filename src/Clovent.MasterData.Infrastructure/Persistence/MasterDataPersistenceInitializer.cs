using Clovent.Platform.Bootstrap;
using Microsoft.EntityFrameworkCore;

namespace Clovent.MasterData.Infrastructure.Persistence;

/// <summary>Applies pending EF Core migrations for <see cref="MasterDataDbContext"/> at startup.</summary>
public sealed class MasterDataPersistenceInitializer(MasterDataDbContext dbContext) : IPersistenceInitializer
{
    /// <inheritdoc/>
    public Task InitializeAsync(CancellationToken cancellationToken = default) =>
        dbContext.Database.MigrateAsync(cancellationToken);
}
