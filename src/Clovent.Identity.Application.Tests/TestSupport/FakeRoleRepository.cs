using Clovent.Identity.Roles;
using Clovent.Identity.Roles.ValueObjects;

namespace Clovent.Identity.Application.Tests.TestSupport;

internal sealed class FakeRoleRepository : IRoleRepository
{
    private readonly Dictionary<RoleId, Role> _roles = [];

    public void Add(Role role) => _roles[role.Id] = role;

    public Task<Role?> GetByIdAsync(RoleId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_roles.GetValueOrDefault(id));

    public Task<Role?> GetByNameAsync(RoleName name, CancellationToken cancellationToken = default) =>
        Task.FromResult(_roles.Values.FirstOrDefault(r => r.Name == name));

    public Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        _roles[role.Id] = role;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Role>> ListAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Role>>([.. _roles.Values]);
}
