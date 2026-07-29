using Clovent.Authentication.Credentials;
using Clovent.Identity.Users;

namespace Clovent.Desktop.Tests.TestSupport;

internal sealed class FakeUserCredentialsRepository : IUserCredentialsRepository
{
    private readonly Dictionary<UserCredentialsId, UserCredentials> _byId = [];

    public Task<UserCredentials?> GetByIdAsync(UserCredentialsId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_byId.GetValueOrDefault(id));

    public Task<UserCredentials?> GetByUserIdAsync(UserId userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_byId.Values.FirstOrDefault(c => c.UserId == userId));

    public Task AddAsync(UserCredentials credentials, CancellationToken cancellationToken = default)
    {
        _byId[credentials.Id] = credentials;
        return Task.CompletedTask;
    }
}
