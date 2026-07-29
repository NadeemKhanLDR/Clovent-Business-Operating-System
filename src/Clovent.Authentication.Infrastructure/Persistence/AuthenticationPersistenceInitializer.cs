using Clovent.Platform.Bootstrap;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Authentication.Infrastructure.Persistence;

/// <summary>Applies pending EF Core migrations for <see cref="AuthenticationDbContext"/> at startup.</summary>
public sealed class AuthenticationPersistenceInitializer(AuthenticationDbContext dbContext) : IPersistenceInitializer
{
    /// <inheritdoc/>
    public Task InitializeAsync(CancellationToken cancellationToken = default) =>
        dbContext.Database.MigrateAsync(cancellationToken);
}
