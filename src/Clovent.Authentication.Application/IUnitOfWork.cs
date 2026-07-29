namespace Clovent.Authentication.Application;

/// <summary>
/// The capability Application-layer command handlers need to commit
/// whatever repository calls and aggregate mutations happened while
/// handling a request, expressed in Application's own vocabulary and owned
/// by Application - the same Dependency Inversion pattern already used for
/// <see cref="IIdentityUserService"/>. No implementation exists yet: this is
/// the seam a future Infrastructure milestone implements against (most
/// plausibly by wrapping an EF Core <c>DbContext.SaveChangesAsync</c>), and
/// a future Application-composition milestone wires into the request
/// pipeline (e.g. a MediatR pipeline behavior, or an explicit call per
/// handler) - see <c>AuthenticationDomain.md</c> Section 10, item 4, which
/// left "does CBOS adopt MediatR pipeline behaviors" as an open question
/// this interface does not itself resolve.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Persists every change made to tracked aggregates and newly-added entities since the last commit.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
