namespace Clovent.Inventory.Application;

/// <summary>The capability Application-layer command handlers need to commit whatever repository calls and aggregate mutations happened while handling a request - mirrors <c>Clovent.Catalog.Application.IUnitOfWork</c>.</summary>
public interface IUnitOfWork
{
    /// <summary>Persists every change made to tracked aggregates and newly-added entities since the last commit.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
