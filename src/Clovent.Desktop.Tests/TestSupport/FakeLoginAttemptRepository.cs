using Clovent.Authentication.LoginAttempts;
using Clovent.Identity.Users;

namespace Clovent.Desktop.Tests.TestSupport;

internal sealed class FakeLoginAttemptRepository : ILoginAttemptRepository
{
    private readonly Dictionary<LoginAttemptId, LoginAttempt> _attempts = [];

    public IReadOnlyCollection<LoginAttempt> All => _attempts.Values.ToList();

    public Task<LoginAttempt?> GetByIdAsync(LoginAttemptId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_attempts.GetValueOrDefault(id));

    public Task<IReadOnlyCollection<LoginAttempt>> GetRecentByIdentifierAsync(string attemptedIdentifier, DateTimeOffset sinceUtc, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<LoginAttempt>>(_attempts.Values
            .Where(a => a.AttemptedIdentifier == attemptedIdentifier && a.OccurredAtUtc >= sinceUtc)
            .OrderByDescending(a => a.OccurredAtUtc)
            .ToList());

    public Task<IReadOnlyCollection<LoginAttempt>> GetRecentByUserIdAsync(UserId userId, DateTimeOffset sinceUtc, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<LoginAttempt>>(_attempts.Values
            .Where(a => a.UserId == userId && a.OccurredAtUtc >= sinceUtc)
            .OrderByDescending(a => a.OccurredAtUtc)
            .ToList());

    public Task AddAsync(LoginAttempt attempt, CancellationToken cancellationToken = default)
    {
        _attempts[attempt.Id] = attempt;
        return Task.CompletedTask;
    }
}
