using Clovent.Identity.Permissions.ValueObjects;

namespace Clovent.Identity.Permissions;

/// <summary>
/// Persistence contract for <see cref="Permission"/> aggregates. No
/// implementation exists yet - this is the seam a future
/// Infrastructure/Persistence milestone implements against.
/// </summary>
public interface IPermissionRepository
{
    /// <summary>Retrieves a permission by identity, or <see langword="null"/> if none exists.</summary>
    Task<Permission?> GetByIdAsync(PermissionId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a permission by code, or <see langword="null"/> if none exists.</summary>
    Task<Permission?> GetByCodeAsync(PermissionCode code, CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created permission.</summary>
    Task AddAsync(Permission permission, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every permission.</summary>
    Task<IReadOnlyList<Permission>> ListAllAsync(CancellationToken cancellationToken = default);
}
