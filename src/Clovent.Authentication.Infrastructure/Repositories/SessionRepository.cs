using Clovent.Authentication.Infrastructure.Persistence;
using Clovent.Authentication.Sessions;
using Clovent.Identity.Users;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Authentication.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="ISessionRepository"/>.</summary>
public sealed class SessionRepository(AuthenticationDbContext dbContext) : ISessionRepository
{
    /// <inheritdoc/>
    public Task<Session?> GetByIdAsync(SessionId id, CancellationToken cancellationToken = default) =>
        dbContext.Sessions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<Session>> GetActiveByUserIdAsync(UserId userId, CancellationToken cancellationToken = default) =>
        await dbContext.Sessions
            .Where(s => s.UserId == userId && s.Status == SessionStatus.Active)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(Session session, CancellationToken cancellationToken = default) =>
        await dbContext.Sessions.AddAsync(session, cancellationToken);
}
