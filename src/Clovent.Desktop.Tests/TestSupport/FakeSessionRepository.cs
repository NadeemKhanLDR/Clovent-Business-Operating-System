using Clovent.Authentication.Sessions;
using Clovent.Identity.Users;

namespace Clovent.Desktop.Tests.TestSupport;

internal sealed class FakeSessionRepository : ISessionRepository
{
    private readonly Dictionary<SessionId, Session> _sessions = [];

    public Task<Session?> GetByIdAsync(SessionId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_sessions.GetValueOrDefault(id));

    public Task<IReadOnlyCollection<Session>> GetActiveByUserIdAsync(UserId userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<Session>>(_sessions.Values
            .Where(s => s.UserId == userId && s.Status == SessionStatus.Active)
            .ToList());

    public Task AddAsync(Session session, CancellationToken cancellationToken = default)
    {
        _sessions[session.Id] = session;
        return Task.CompletedTask;
    }
}
