using Clovent.Authentication.Infrastructure.Persistence;
using Clovent.Authentication.LoginAttempts;
using Clovent.Identity.Users;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Authentication.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="ILoginAttemptRepository"/>.</summary>
public sealed class LoginAttemptRepository(AuthenticationDbContext dbContext) : ILoginAttemptRepository
{
    /// <inheritdoc/>
    public Task<LoginAttempt?> GetByIdAsync(LoginAttemptId id, CancellationToken cancellationToken = default) =>
        dbContext.LoginAttempts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<LoginAttempt>> GetRecentByIdentifierAsync(
        string attemptedIdentifier,
        DateTimeOffset sinceUtc,
        CancellationToken cancellationToken = default) =>
        await dbContext.LoginAttempts
            .Where(a => a.AttemptedIdentifier == attemptedIdentifier && a.OccurredAtUtc >= sinceUtc)
            .OrderByDescending(a => a.OccurredAtUtc)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<LoginAttempt>> GetRecentByUserIdAsync(
        UserId userId,
        DateTimeOffset sinceUtc,
        CancellationToken cancellationToken = default) =>
        await dbContext.LoginAttempts
            .Where(a => a.UserId == userId && a.OccurredAtUtc >= sinceUtc)
            .OrderByDescending(a => a.OccurredAtUtc)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(LoginAttempt attempt, CancellationToken cancellationToken = default) =>
        await dbContext.LoginAttempts.AddAsync(attempt, cancellationToken);
}
