using Clovent.Authentication.RefreshSessions;
using Clovent.Authentication.Sessions;

namespace Clovent.Desktop.Tests.TestSupport;

internal sealed class FakeRefreshSessionRepository : IRefreshSessionRepository
{
    private readonly Dictionary<RefreshSessionId, RefreshSession> _refreshSessions = [];

    public IReadOnlyCollection<RefreshSession> All => _refreshSessions.Values.ToList();

    public Task<RefreshSession?> GetByIdAsync(RefreshSessionId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_refreshSessions.GetValueOrDefault(id));

    public Task<RefreshSession?> GetActiveBySessionIdAsync(SessionId sessionId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_refreshSessions.Values.FirstOrDefault(r => r.SessionId == sessionId && r.Status == RefreshSessionStatus.Active));

    public Task AddAsync(RefreshSession refreshSession, CancellationToken cancellationToken = default)
    {
        _refreshSessions[refreshSession.Id] = refreshSession;
        return Task.CompletedTask;
    }
}
