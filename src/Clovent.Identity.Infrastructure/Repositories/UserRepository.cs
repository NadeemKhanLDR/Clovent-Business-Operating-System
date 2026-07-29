using Clovent.Identity.Infrastructure.Persistence;
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
}
