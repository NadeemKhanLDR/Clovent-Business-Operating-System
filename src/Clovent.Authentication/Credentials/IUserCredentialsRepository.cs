using Clovent.Identity.Users;

namespace Clovent.Authentication.Credentials;

/// <summary>
/// Persistence contract for <see cref="UserCredentials"/>. No implementation
/// exists yet - this is the seam a future Infrastructure/Persistence
/// milestone implements against.
/// </summary>
public interface IUserCredentialsRepository
{
    /// <summary>Retrieves a user's credential record by identity, or <see langword="null"/> if none exists.</summary>
    Task<UserCredentials?> GetByIdAsync(UserCredentialsId id, CancellationToken cancellationToken = default);

    /// <summary>Retrieves a user's credential record by their <see cref="UserId"/>, or <see langword="null"/> if none exists.</summary>
    Task<UserCredentials?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves every credential record. Used only by PIN administration and
    /// PIN-only sign-in, which must resolve a user FROM a PIN - hashes are
    /// salted, so there is no indexable representation to look a PIN up by,
    /// only verification against each record in turn.
    /// </summary>
    Task<IReadOnlyList<UserCredentials>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a newly-created credential record.</summary>
    Task AddAsync(UserCredentials credentials, CancellationToken cancellationToken = default);
}
