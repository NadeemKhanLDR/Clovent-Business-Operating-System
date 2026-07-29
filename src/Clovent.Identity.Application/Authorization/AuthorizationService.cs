using Clovent.Identity.Permissions;
using Clovent.Identity.Roles;
using Clovent.Identity.Users;

namespace Clovent.Identity.Application.Authorization;

/// <summary><see cref="IAuthorizationService"/> implementation - the only place permission/role evaluation logic lives.</summary>
public sealed class AuthorizationService(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IPermissionRepository permissionRepository,
    IPermissionCache cache,
    IAuthorizationPolicyProvider policyProvider) : IAuthorizationService
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<string>> GetPermissionCodesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cached = await cache.GetAsync(userId, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var user = await userRepository.GetByIdAsync(new UserId(userId), cancellationToken);
        if (user is null)
        {
            return [];
        }

        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var roleId in user.RoleIds)
        {
            var role = await roleRepository.GetByIdAsync(roleId, cancellationToken);
            if (role is null)
            {
                continue;
            }

            foreach (var permissionId in role.PermissionIds)
            {
                var permission = await permissionRepository.GetByIdAsync(permissionId, cancellationToken);
                if (permission is not null)
                {
                    codes.Add(permission.Code.Value);
                }
            }
        }

        var result = codes.ToList();
        await cache.SetAsync(userId, result, cancellationToken);
        return result;
    }

    /// <inheritdoc/>
    public async Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken cancellationToken = default)
    {
        var codes = await GetPermissionCodesAsync(userId, cancellationToken);
        return codes.Contains(permissionCode, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public async Task<bool> HasRoleAsync(Guid userId, string roleName, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(new UserId(userId), cancellationToken);
        if (user is null)
        {
            return false;
        }

        foreach (var roleId in user.RoleIds)
        {
            var role = await roleRepository.GetByIdAsync(roleId, cancellationToken);
            if (role is not null && string.Equals(role.Name.Value, roleName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public async Task<bool> SatisfiesPolicyAsync(Guid userId, string policyName, CancellationToken cancellationToken = default)
    {
        var policy = policyProvider.GetPolicy(policyName);
        if (policy is null)
        {
            return false;
        }

        var codes = await GetPermissionCodesAsync(userId, cancellationToken);
        return policy.RequiredPermissionCodes.All(code => codes.Contains(code, StringComparer.OrdinalIgnoreCase));
    }
}
