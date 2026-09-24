using Clovent.Authentication.Credentials;
using Clovent.Authentication.Infrastructure.Persistence;
using Clovent.Identity.Users;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Authentication.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IUserCredentialsRepository"/>.</summary>
public sealed class UserCredentialsRepository(AuthenticationDbContext dbContext) : IUserCredentialsRepository
{
    /// <inheritdoc/>
    public async Task<UserCredentials?> GetByIdAsync(UserCredentialsId id, CancellationToken cancellationToken = default)
    {
        var local = dbContext.UserCredentials.Local.FirstOrDefault(c => c.Id == id);
        if (local is not null)
        {
            return local;
        }

        return await dbContext.UserCredentials.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<UserCredentials?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var local = dbContext.UserCredentials.Local.FirstOrDefault(c => c.UserId == userId);
        if (local is not null)
        {
            return local;
        }

        return await dbContext.UserCredentials.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<UserCredentials>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var dbItems = await dbContext.UserCredentials.ToListAsync(cancellationToken);
        var localNewItems = dbContext.UserCredentials.Local.Where(c => !dbItems.Any(db => db.Id == c.Id)).ToList();
        return localNewItems.Count == 0 ? dbItems : [.. dbItems, .. localNewItems];
    }

    /// <inheritdoc/>
    public async Task AddAsync(UserCredentials credentials, CancellationToken cancellationToken = default) =>
        await dbContext.UserCredentials.AddAsync(credentials, cancellationToken);
}
