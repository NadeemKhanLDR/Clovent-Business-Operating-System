using Clovent.Restaurant.Application;

namespace Clovent.Restaurant.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IUnitOfWork"/>, wrapping <see cref="RestaurantDbContext"/>'s <c>SaveChangesAsync</c>. Mirrors <c>Clovent.Catalog.Infrastructure.Persistence.UnitOfWork</c>.</summary>
public sealed class UnitOfWork(RestaurantDbContext dbContext) : IUnitOfWork
{
    /// <inheritdoc/>
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // A failed or unacknowledged commit must never leave staged entities
            // available to the next command (including duplicate-key recovery).
            DiscardTrackedState();
            throw;
        }
    }

    internal void DiscardTrackedState()
    {
        foreach (var entry in dbContext.ChangeTracker.Entries().ToArray())
        {
            // The worker owns its already-claimed message independently of a
            // nested inventory command. Keep its original concurrency token;
            // detaching it would make a subsequent retry-status update compare
            // against the new, unsaved domain version instead of the claim.
            if (entry.Entity is Clovent.Restaurant.Outbox.OutboxMessage &&
                entry.State == Microsoft.EntityFrameworkCore.EntityState.Unchanged)
                continue;
            entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
        }
    }
}
