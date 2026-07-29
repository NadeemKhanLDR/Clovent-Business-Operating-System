using Clovent.Authentication.Infrastructure.Persistence;
using Clovent.Authentication.RefreshSessions;
using Clovent.Authentication.Sessions;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Authentication.Infrastructure.Repositories;

/// <summary>EF Core implementation of <see cref="IRefreshSessionRepository"/>.</summary>
public sealed class RefreshSessionRepository(AuthenticationDbContext dbContext) : IRefreshSessionRepository
{
    /// <inheritdoc/>
    public Task<RefreshSession?> GetByIdAsync(RefreshSessionId id, CancellationToken cancellationToken = default) =>
        dbContext.RefreshSessions.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    /// <inheritdoc/>
    public Task<RefreshSession?> GetActiveBySessionIdAsync(SessionId sessionId, CancellationToken cancellationToken = default) =>
        dbContext.RefreshSessions.FirstOrDefaultAsync(
            r => r.SessionId == sessionId && r.Status == RefreshSessionStatus.Active,
            cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(RefreshSession refreshSession, CancellationToken cancellationToken = default) =>
        await dbContext.RefreshSessions.AddAsync(refreshSession, cancellationToken);
}
