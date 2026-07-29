namespace Clovent.Identity.Application;

/// <summary>
/// The capability Application-layer command handlers need to commit
/// whatever repository calls and aggregate mutations happened while
/// handling a request - the same seam already established by
/// <c>Clovent.Authentication.Application.IUnitOfWork</c>. A future
/// Infrastructure milestone implements this (wrapping an EF Core
/// <c>DbContext.SaveChangesAsync</c>) and registers it into the MediatR
/// pipeline via an open-generic <c>UnitOfWorkBehavior</c>, mirroring
/// Authentication's pattern.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Persists every change made to tracked aggregates and newly-added entities since the last commit.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
