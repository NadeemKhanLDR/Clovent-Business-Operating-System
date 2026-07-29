using Clovent.Desktop.Theming;
using Clovent.Identity.Infrastructure.Persistence;
using Clovent.Identity.Permissions;
using Clovent.Identity.Permissions.ValueObjects;
using Clovent.Identity.Roles;
using Clovent.Identity.Roles.ValueObjects;
using Clovent.Identity.Users;
using Clovent.Identity.Users.ValueObjects;
using Clovent.Platform.Bootstrap;
using Microsoft.Extensions.Options;

namespace Clovent.Desktop.Seed;

/// <summary>
/// Development-only convenience: grants the demo admin user (seeded by
/// <see cref="DevelopmentUserSeedStartupTask"/>, which must run first - see
/// its registration order in <c>DesktopServiceCollectionExtensions</c>) an
/// "Administrator" role holding every menu/feature permission this
/// milestone's screens check. Without this,
/// <c>Clovent.Identity.Application.Authorization.AuthorizationService</c>'s
/// deliberate no-bypass design (every permission check requires an actual
/// granted permission) would leave every Milestone 13
/// screen invisible and every button disabled for the demo user, since a
/// freshly-seeded user starts with zero roles. Gated by the same
/// <see cref="DesktopOptions.SeedDevelopmentUser"/> flag - this is that
/// seed's natural completion, not a separate concern.
/// </summary>
public sealed class DevelopmentAuthorizationSeedStartupTask(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IPermissionRepository permissionRepository,
    IdentityDbContext identityDbContext,
    IOptions<DesktopOptions> options) : IStartupTask
{
    private const string AdministratorRoleName = "Administrator";

    private static readonly string[] MenuKeys =
    [
        "dashboard", "organizations", "companies", "branches", "departments",
        "warehouses", "terminals", "fiscalyears", "currencies", "businesssettings",
        "categories", "brands", "units", "products", "variants", "barcodes", "prices",
        "warehousestocks", "stockadjustments", "stocktransfers", "inventorytransactions",

        // Milestone 15 ("Restaurant POS Core").
        "diningareas", "tables", "pos", "runningorders", "holdorders", "kitchentickets",
    ];

    private static readonly (string Feature, string[] Operations)[] FeatureOperations =
    [
        ("organizations", ["create", "edit", "activate", "deactivate"]),
        ("companies", ["create", "edit", "activate", "deactivate"]),
        ("branches", ["create", "edit", "activate", "deactivate"]),
        ("departments", ["create", "edit", "activate", "deactivate"]),
        ("warehouses", ["create", "edit", "activate", "deactivate"]),
        ("terminals", ["create", "edit", "activate", "deactivate"]),
        ("fiscalyears", ["create", "edit", "deactivate"]),
        ("currencies", ["create", "activate", "deactivate"]),
        ("businesssettings", ["edit"]),

        // Milestone 14 ("Product Catalog & Inventory Foundation").
        ("categories", ["create", "edit", "activate", "deactivate"]),
        ("brands", ["create", "edit", "activate", "deactivate"]),
        ("units", ["create", "edit", "activate", "deactivate"]),
        ("products", ["create", "edit", "activate", "deactivate"]),
        ("variants", ["create", "edit", "activate", "deactivate"]),
        ("barcodes", ["create", "activate", "deactivate", "markprimary"]),
        ("prices", ["create", "edit", "activate", "deactivate"]),
        ("warehousestocks", ["create", "edit", "receive", "issue", "reserve", "release"]),
        ("stockadjustments", ["create", "apply", "cancel"]),
        ("stocktransfers", ["create", "complete", "cancel"]),
        ("inventorytransactions", ["view"]),

        // Milestone 15 ("Restaurant POS Core").
        ("diningareas", ["create", "edit", "activate", "deactivate"]),
        ("tables", ["create", "edit", "activate", "deactivate", "occupy", "vacate", "reserve", "outofservice", "returntoservice"]),
        ("pos", ["create", "hold", "resume", "void", "cancel", "reopen", "sendtokitchen", "complete", "pay",
            "transfertable", "mergetables", "splitbill", "notes", "discount", "servicecharge", "additem", "editline"]),
        ("kitchentickets", ["start", "markready", "serve", "cancel"]),
    ];

    /// <inheritdoc/>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!options.Value.SeedDevelopmentUser)
        {
            return;
        }

        var user = await userRepository.GetByUserNameAsync(UserName.Create("admin"), cancellationToken);
        if (user is null || user.RoleIds.Count > 0)
        {
            return;
        }

        var permissionIds = new List<PermissionId>();
        foreach (var code in BuildPermissionCodes())
        {
            var permission = Permission.Create(PermissionCode.Create(code), $"Grants {code}.");
            await permissionRepository.AddAsync(permission, cancellationToken);
            permissionIds.Add(permission.Id);
        }

        var role = Role.Create(RoleName.Create(AdministratorRoleName));
        foreach (var permissionId in permissionIds)
        {
            role.AddPermission(permissionId);
        }

        await roleRepository.AddAsync(role, cancellationToken);

        user.AssignRole(role.Id);

        await identityDbContext.SaveChangesAsync(cancellationToken);
    }

    private static IEnumerable<string> BuildPermissionCodes()
    {
        foreach (var key in MenuKeys)
        {
            yield return $"menu.{key}";
        }

        foreach (var (feature, operations) in FeatureOperations)
        {
            foreach (var operation in operations)
            {
                yield return $"feature.{feature}.{operation}";
            }
        }
    }
}
