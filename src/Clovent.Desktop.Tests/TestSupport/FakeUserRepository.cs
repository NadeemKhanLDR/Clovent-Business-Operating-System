using Clovent.Identity.Users;
using Clovent.Identity.Users.ValueObjects;

namespace Clovent.Desktop.Tests.TestSupport;

internal sealed class FakeUserRepository : IUserRepository
{
    private readonly Dictionary<UserId, User> _users = [];

    public void Add(User user) => _users[user.Id] = user;

    public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.GetValueOrDefault(id));

    public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.Values.FirstOrDefault(u => u.Email == email));

    public Task<User?> GetByUserNameAsync(UserName userName, CancellationToken cancellationToken = default) =>
        Task.FromResult(_users.Values.FirstOrDefault(u => u.UserName == userName));

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        _users[user.Id] = user;
        return Task.CompletedTask;
    }
}
