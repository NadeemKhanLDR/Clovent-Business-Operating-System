using Clovent.Identity.Branches;
using Clovent.Identity.Companies;
using Clovent.Identity.Roles;
using Clovent.Identity.Users;
using Clovent.Identity.Users.ValueObjects;

namespace Clovent.Identity.Application.Tests.TestSupport;

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

    public Task<IReadOnlyList<User>> SearchAsync(
        string? searchText = null,
        CompanyId? companyId = null,
        BranchId? branchId = null,
        RoleId? roleId = null,
        UserStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var users = _users.Values.AsEnumerable();

        if (companyId is { } company)
            users = users.Where(u => u.CompanyId == company);
        if (branchId is { } branch)
            users = users.Where(u => u.BranchId == branch);
        if (roleId is { } role)
            users = users.Where(u => u.RoleIds.Contains(role));
        if (status is { } userStatus)
            users = users.Where(u => u.Status == userStatus);
        if (!string.IsNullOrWhiteSpace(searchText))
            users = users.Where(u =>
                u.UserName.Value.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                u.DisplayName.Value.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                u.Email.Value.Contains(searchText, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult<IReadOnlyList<User>>([.. users]);
    }
}
