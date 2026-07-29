using Clovent.Identity.Users.ValueObjects;

namespace Clovent.Identity.Users;

/// <summary>
/// Persistence contract for <see cref="User"/> aggregates. No implementation
/// exists yet - this is the seam a future Infrastructure/Persistence
/// milestone implements against.
/// </summary>
public interface IUserRepository
{
    /// <summary>Retrieves a user by identity, or <see langword="null"/> if none exists.</summary>
    Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a user by email address, or <see langword="null"/> if none exists.</summary>
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a user by login handle, or <see langword="null"/> if none exists.</summary>
    Task<User?> GetByUserNameAsync(UserName userName, CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created user.</summary>
    Task AddAsync(User user, CancellationToken cancellationToken = default);
}
