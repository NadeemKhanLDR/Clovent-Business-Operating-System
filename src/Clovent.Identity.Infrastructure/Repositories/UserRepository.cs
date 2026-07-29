using Clovent.Identity.Branches;
using Clovent.Identity.Companies;
using Clovent.Identity.Infrastructure.Persistence;
using Clovent.Identity.Roles;
using Clovent.Identity.Users;
using Clovent.Identity.Users.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Identity.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IUserRepository"/>.</summary>
public sealed class UserRepository(IdentityDbContext dbContext) : IUserRepository
{
    /// <inheritdoc/>
    public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) =>
        dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    /// <inheritdoc/>
    public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default) =>
        dbContext.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    /// <inheritdoc/>
    public Task<User?> GetByUserNameAsync(UserName userName, CancellationToken cancellationToken = default) =>
        dbContext.Users.FirstOrDefaultAsync(u => u.UserName == userName, cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(User user, CancellationToken cancellationToken = default) =>
        await dbContext.Users.AddAsync(user, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<User>> SearchAsync(
        string? searchText = null,
        CompanyId? companyId = null,
        BranchId? branchId = null,
        RoleId? roleId = null,
        UserStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Users.AsQueryable();

        if (companyId is { } company)
            query = query.Where(u => u.CompanyId == company);

        if (branchId is { } branch)
            query = query.Where(u => u.BranchId == branch);

        if (status is { } userStatus)
            query = query.Where(u => u.Status == userStatus);

        // RoleIds is a converted JSON column - filter client-side once the
        // narrower server-side predicates above have already reduced the set,
        // same reasoning MemoryPermissionCache applies to permission lookups.
        var users = await query.ToListAsync(cancellationToken);

        if (roleId is { } role)
            users = [.. users.Where(u => u.RoleIds.Contains(role))];

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var trimmed = searchText.Trim();
            users =
            [
                .. users.Where(u =>
                    u.UserName.Value.Contains(trimmed, StringComparison.OrdinalIgnoreCase) ||
                    u.DisplayName.Value.Contains(trimmed, StringComparison.OrdinalIgnoreCase) ||
                    u.Email.Value.Contains(trimmed, StringComparison.OrdinalIgnoreCase)),
            ];
        }

        return users;
    }
}
