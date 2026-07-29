using Clovent.Identity.Application.Authorization;

namespace Clovent.Identity.Application.Tests.TestSupport;

/// <summary>Records the permission code it was last asked to check, so tests can assert exactly which code a policy wrapper produced.</summary>
internal sealed class RecordingAuthorizationService : IAuthorizationService
{
    public string? LastCheckedPermissionCode { get; private set; }
    public bool Result { get; set; } = true;

    public Task<IReadOnlyCollection<string>> GetPermissionCodesAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<string>>([]);

    public Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken cancellationToken = default)
    {
        LastCheckedPermissionCode = permissionCode;
        return Task.FromResult(Result);
    }

    public Task<bool> HasRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<bool> SatisfiesPolicyAsync(Guid userId, string policyName, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
}
