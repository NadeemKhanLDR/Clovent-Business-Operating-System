namespace Clovent.Identity.Application.Authorization;

/// <summary>
/// Caches a user's resolved permission-code set. Resolving it from scratch
/// means a repository round trip per assigned role and per granted
/// permission - cheap for one user, needlessly repeated for every
/// authorization check the same user triggers (a menu render, a feature
/// gate) without one. Owned as an Application-layer seam (the same
/// Dependency Inversion pattern as Authentication's <c>IUnitOfWork</c>);
/// implemented in Infrastructure since the caching *technology* is an
/// implementation detail, not a policy decision.
/// </summary>
public interface IPermissionCache
{
    /// <summary>The cached permission codes for a user, or <see langword="null"/> on a cache miss.</summary>
    Task<IReadOnlyCollection<string>?> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Caches the resolved permission codes for a user.</summary>
    Task SetAsync(Guid userId, IReadOnlyCollection<string> permissionCodes, CancellationToken cancellationToken = default);

    /// <summary>Evicts a user's cached permission codes - call after any role/permission assignment changes.</summary>
    Task InvalidateAsync(Guid userId, CancellationToken cancellationToken = default);
}
