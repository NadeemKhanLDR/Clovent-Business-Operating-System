using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Identity.Application.Authorization;
using Clovent.Identity.Infrastructure.Persistence;
using Clovent.Identity.Permissions;
using Clovent.Identity.Permissions.ValueObjects;
using Clovent.Identity.Roles;
using Clovent.Identity.Roles.ValueObjects;
using Clovent.Platform.Bootstrap;

namespace Clovent.Desktop.Security;

/// <summary>
/// Production-grade startup synchronization task that ensures the authoritative
/// <see cref="SystemPermissionCatalog"/> exists in the database and backfills
/// newly introduced permissions (such as <c>menu.printerhealth</c>, <c>menu.auditanalytics</c>,
/// <c>feature.pos.refund</c>, and tax/refund reporting capabilities) onto existing
/// <c>Administrator</c> and <c>Manager</c> roles during version upgrades.
/// </summary>
/// <remarks>
/// Strictly preserves fail-closed security: never creates demo users, never elevates
/// unauthorized roles (such as Cashier), and operates independently of development seed flags.
/// </remarks>
public sealed class ProductionPermissionCatalogSynchronizationStartupTask(
    IRoleRepository roleRepository,
    IPermissionRepository permissionRepository,
    IdentityDbContext identityDbContext) : IStartupTask
{
    private static readonly string[] AdminRoleNames = ["Administrator", "Admin", "SuperAdmin"];
    private const string ManagerRoleName = "Manager";

    /// <inheritdoc/>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var changed = false;
        var permissionsByCode = new Dictionary<string, Permission>(StringComparer.OrdinalIgnoreCase);

        foreach (var code in SystemPermissionCatalog.GetAllPermissionCodes())
        {
            var permissionCode = PermissionCode.Create(code);
            var permission = await permissionRepository.GetByCodeAsync(permissionCode, cancellationToken).ConfigureAwait(false);
            if (permission is null)
            {
                permission = Permission.Create(permissionCode, $"Grants {code}.");
                await permissionRepository.AddAsync(permission, cancellationToken).ConfigureAwait(false);
                changed = true;
            }

            permissionsByCode[code] = permission;
        }

        // Backfill existing Administrator-class roles with full catalog permissions
        foreach (var adminName in AdminRoleNames)
        {
            var adminRole = await roleRepository.GetByNameAsync(RoleName.Create(adminName), cancellationToken).ConfigureAwait(false);
            if (adminRole is not null)
            {
                foreach (var permission in permissionsByCode.Values)
                {
                    if (!adminRole.PermissionIds.Contains(permission.Id))
                    {
                        adminRole.AddPermission(permission.Id);
                        changed = true;
                    }
                }
            }
        }

        // Backfill existing Manager role with baseline manager permissions
        var managerRole = await roleRepository.GetByNameAsync(RoleName.Create(ManagerRoleName), cancellationToken).ConfigureAwait(false);
        if (managerRole is not null)
        {
            foreach (var managerCode in SystemPermissionCatalog.GetManagerPermissionCodes())
            {
                if (permissionsByCode.TryGetValue(managerCode, out var permission) &&
                    !managerRole.PermissionIds.Contains(permission.Id))
                {
                    managerRole.AddPermission(permission.Id);
                    changed = true;
                }
            }
        }

        if (changed)
        {
            await identityDbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
