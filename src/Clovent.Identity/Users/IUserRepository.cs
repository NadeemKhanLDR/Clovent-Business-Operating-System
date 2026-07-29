using Clovent.Identity.Branches;
using Clovent.Identity.Companies;
using Clovent.Identity.Roles;
using Clovent.Identity.Users.ValueObjects;

namespace Clovent.Identity.Users;

/// <summary>
/// Persistence contract for <see cref="User"/> aggregates.
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

    /// <summary>
    /// Retrieves users matching every supplied filter (all optional/AND'ed
    /// together) - <paramref name="searchText"/> matches against username,
    /// display name, or email.
    /// </summary>
    Task<IReadOnlyList<User>> SearchAsync(
        string? searchText = null,
        CompanyId? companyId = null,
        BranchId? branchId = null,
        RoleId? roleId = null,
        UserStatus? status = null,
        CancellationToken cancellationToken = default);
}
