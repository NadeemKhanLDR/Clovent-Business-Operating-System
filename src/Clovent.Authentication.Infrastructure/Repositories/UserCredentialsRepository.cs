using Clovent.Authentication.Credentials;
using Clovent.Authentication.Infrastructure.Persistence;
using Clovent.Identity.Users;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Authentication.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IUserCredentialsRepository"/>.</summary>
public sealed class UserCredentialsRepository(AuthenticationDbContext dbContext) : IUserCredentialsRepository
{
    /// <inheritdoc/>
    public Task<UserCredentials?> GetByIdAsync(UserCredentialsId id, CancellationToken cancellationToken = default) =>
        dbContext.UserCredentials.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    /// <inheritdoc/>
    public Task<UserCredentials?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default) =>
        dbContext.UserCredentials.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(UserCredentials credentials, CancellationToken cancellationToken = default) =>
        await dbContext.UserCredentials.AddAsync(credentials, cancellationToken);
}
