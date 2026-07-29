using Clovent.Identity.Permissions;
using Clovent.Identity.Permissions.ValueObjects;

namespace Clovent.Identity.Application.Tests.TestSupport;

internal sealed class FakePermissionRepository : IPermissionRepository
{
    private readonly Dictionary<PermissionId, Permission> _permissions = [];

    public void Add(Permission permission) => _permissions[permission.Id] = permission;

    public Task<Permission?> GetByIdAsync(PermissionId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_permissions.GetValueOrDefault(id));

    public Task<Permission?> GetByCodeAsync(PermissionCode code, CancellationToken cancellationToken = default) =>
        Task.FromResult(_permissions.Values.FirstOrDefault(p => p.Code == code));

    public Task AddAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        _permissions[permission.Id] = permission;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Permission>> ListAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Permission>>([.. _permissions.Values]);
}
