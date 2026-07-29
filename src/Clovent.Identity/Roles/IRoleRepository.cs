using Clovent.Identity.Roles.ValueObjects;

namespace Clovent.Identity.Roles;

/// <summary>
/// Persistence contract for <see cref="Role"/> aggregates. No implementation
/// exists yet - this is the seam a future Infrastructure/Persistence
/// milestone implements against.
/// </summary>
public interface IRoleRepository
{
    /// <summary>Retrieves a role by identity, or <see langword="null"/> if none exists.</summary>
    Task<Role?> GetByIdAsync(RoleId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a role by name, or <see langword="null"/> if none exists.</summary>
    Task<Role?> GetByNameAsync(RoleName name, CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created role.</summary>
    Task AddAsync(Role role, CancellationToken cancellationToken = default);

    /// <summary>Retrieves every role.</summary>
    Task<IReadOnlyList<Role>> ListAllAsync(CancellationToken cancellationToken = default);
}
