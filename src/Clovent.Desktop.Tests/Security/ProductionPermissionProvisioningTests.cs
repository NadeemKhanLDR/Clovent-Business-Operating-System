using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Clovent.Authentication.Application;
using Clovent.Authentication.Infrastructure.Persistence;
using Clovent.Authentication.Infrastructure.Security;
using Clovent.Desktop.Commissioning.Services;
using Clovent.Desktop.Navigation;
using Clovent.Desktop.Security;
using Clovent.Identity.Application.Authorization;
using Clovent.Identity.Infrastructure.Persistence;
using Clovent.Identity.Infrastructure.Repositories;
using Clovent.Identity.Permissions;
using Clovent.Identity.Permissions.ValueObjects;
using Clovent.Identity.Roles;
using Clovent.Identity.Roles.ValueObjects;
using Clovent.Identity.Users;
using Clovent.Identity.Users.ValueObjects;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Desktop.Tests.Security;

public sealed class ProductionPermissionProvisioningTests : IDisposable
{
    private readonly SqliteConnection _identityConn;
    private readonly SqliteConnection _authConn;
    private readonly DbContextOptions<IdentityDbContext> _identityOptions;
    private readonly DbContextOptions<AuthenticationDbContext> _authOptions;

    public ProductionPermissionProvisioningTests()
    {
        _identityConn = new SqliteConnection("DataSource=:memory:");
        _identityConn.Open();
        _authConn = new SqliteConnection("DataSource=:memory:");
        _authConn.Open();

        _identityOptions = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlite(_identityConn)
            .Options;

        _authOptions = new DbContextOptionsBuilder<AuthenticationDbContext>()
            .UseSqlite(_authConn)
            .Options;

        using var idDb = new IdentityDbContext(_identityOptions);
        idDb.Database.EnsureCreated();

        using var authDb = new AuthenticationDbContext(_authOptions);
        authDb.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _identityConn.Dispose();
        _authConn.Dispose();
    }

    [Fact]
    public void NavigationRegistry_AllItems_MatchSystemPermissionCatalogExactly()
    {
        var catalogCodes = new HashSet<string>(
            SystemPermissionCatalog.GetAllPermissionCodes(),
            StringComparer.OrdinalIgnoreCase);

        foreach (var item in NavigationRegistry.AllItems)
        {
            Assert.Contains($"menu.{item.Key}", catalogCodes);
            Assert.Contains(item.Permission, catalogCodes);
        }
    }

    [Fact]
    public async Task FirstAdminProvisioning_ProvisionsPrinterHealthAndAuditAnalytics_ForAdminAndManager()
    {
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped(_ => new IdentityDbContext(_identityOptions));
        services.AddScoped(_ => new AuthenticationDbContext(_authOptions));

        using var sp = services.BuildServiceProvider();
        var sut = new FirstAdminProvisioningService();

        var (success, _, adminUserId) = await sut.ProvisionFirstAdminAsync(sp, new FirstAdminProvisioningRequest
        {
            UserName = "prodadmin",
            DisplayName = "Prod Admin",
            Email = "prodadmin@clovent.local",
            Password = "ProdAdmin!2026#Secure",
            ConfirmPassword = "ProdAdmin!2026#Secure"
        });

        Assert.True(success);
        Assert.NotNull(adminUserId);

        using var db = new IdentityDbContext(_identityOptions);
        var userRepo = new UserRepository(db);
        var roleRepo = new RoleRepository(db);
        var permRepo = new PermissionRepository(db);
        var authService = new AuthorizationService(
            userRepo,
            roleRepo,
            permRepo,
            new NullPermissionCache(),
            new AuthorizationPolicyProvider());
        var menuPolicy = new MenuAuthorizationPolicy(authService);

        // Verify Administrator has access to Printer Health, Audit Analytics, Refund Reports, Tax Profiles, and POS Refund
        Assert.True(await menuPolicy.CanViewMenuItemAsync(adminUserId.Value, "printerhealth"));
        Assert.True(await menuPolicy.CanViewMenuItemAsync(adminUserId.Value, "auditanalytics"));
        Assert.True(await menuPolicy.CanViewMenuItemAsync(adminUserId.Value, "refundreports"));
        Assert.True(await menuPolicy.CanViewMenuItemAsync(adminUserId.Value, "taxprofiles"));
        Assert.True(await authService.HasPermissionAsync(adminUserId.Value, "feature.pos.refund"));

        // Create a Manager user assigned to the provisioned Manager role
        var managerRole = await roleRepo.GetByNameAsync(RoleName.Create("Manager"));
        Assert.NotNull(managerRole);

        var managerUser = User.Create(
            Email.Create("manager@clovent.local"),
            UserName.Create("storemanager"),
            DisplayName.Create("Store Manager"));
        managerUser.Activate();
        managerUser.AssignRole(managerRole.Id);

        // Create an unauthorized Cashier user with only menu.pos
        var cashierRole = Role.Create(RoleName.Create("Cashier"));
        var posPerm = await permRepo.GetByCodeAsync(PermissionCode.Create("menu.pos"));
        Assert.NotNull(posPerm);
        cashierRole.AddPermission(posPerm.Id);
        await roleRepo.AddAsync(cashierRole);

        var cashierUser = User.Create(
            Email.Create("cashier@clovent.local"),
            UserName.Create("frontcashier"),
            DisplayName.Create("Front Cashier"));
        cashierUser.Activate();
        cashierUser.AssignRole(cashierRole.Id);

        await userRepo.AddAsync(managerUser);
        await userRepo.AddAsync(cashierUser);
        await db.SaveChangesAsync();

        // Manager has access to Printer Health, Audit Analytics, and POS Refund
        Assert.True(await menuPolicy.CanViewMenuItemAsync(managerUser.Id.Value, "printerhealth"));
        Assert.True(await menuPolicy.CanViewMenuItemAsync(managerUser.Id.Value, "auditanalytics"));
        Assert.True(await authService.HasPermissionAsync(managerUser.Id.Value, "feature.pos.refund"));

        // Cashier is strictly denied (fail-closed)
        Assert.True(await menuPolicy.CanViewMenuItemAsync(cashierUser.Id.Value, "pos"));
        Assert.False(await menuPolicy.CanViewMenuItemAsync(cashierUser.Id.Value, "printerhealth"));
        Assert.False(await menuPolicy.CanViewMenuItemAsync(cashierUser.Id.Value, "auditanalytics"));
        Assert.False(await authService.HasPermissionAsync(cashierUser.Id.Value, "feature.pos.refund"));
    }

    [Fact]
    public async Task ProductionUpgradeSyncTask_BackfillsMissingPermissions_WithoutSeederOrElevatingCashier()
    {
        // Simulate an older production database before CBOS 1.3.0 upgrade:
        // Only basic menu.pos exists; Administrator, Manager, and Cashier roles exist without new 1.3.0 permissions.
        Guid adminUserId;
        Guid managerUserId;
        Guid cashierUserId;

        using (var setupDb = new IdentityDbContext(_identityOptions))
        {
            var posPerm = Permission.Create(PermissionCode.Create("menu.pos"), "Grants menu.pos.");
            await setupDb.Permissions.AddAsync(posPerm);

            var adminRole = Role.Create(RoleName.Create("Administrator"));
            adminRole.AddPermission(posPerm.Id);

            var managerRole = Role.Create(RoleName.Create("Manager"));
            managerRole.AddPermission(posPerm.Id);

            var cashierRole = Role.Create(RoleName.Create("Cashier"));
            cashierRole.AddPermission(posPerm.Id);

            await setupDb.Roles.AddRangeAsync(adminRole, managerRole, cashierRole);

            var admin = User.Create(Email.Create("admin@upgrade.local"), UserName.Create("admin_up"), DisplayName.Create("Admin"));
            admin.Activate();
            admin.AssignRole(adminRole.Id);

            var manager = User.Create(Email.Create("mgr@upgrade.local"), UserName.Create("mgr_up"), DisplayName.Create("Manager"));
            manager.Activate();
            manager.AssignRole(managerRole.Id);

            var cashier = User.Create(Email.Create("csh@upgrade.local"), UserName.Create("csh_up"), DisplayName.Create("Cashier"));
            cashier.Activate();
            cashier.AssignRole(cashierRole.Id);

            await setupDb.Users.AddRangeAsync(admin, manager, cashier);
            await setupDb.SaveChangesAsync();

            adminUserId = admin.Id.Value;
            managerUserId = manager.Id.Value;
            cashierUserId = cashier.Id.Value;
        }

        // Execute ProductionPermissionCatalogSynchronizationStartupTask (no development seeder!)
        using (var syncDb = new IdentityDbContext(_identityOptions))
        {
            var roleRepo = new RoleRepository(syncDb);
            var permRepo = new PermissionRepository(syncDb);
            var syncTask = new ProductionPermissionCatalogSynchronizationStartupTask(roleRepo, permRepo, syncDb);
            await syncTask.ExecuteAsync();
        }

        // Verify post-upgrade authorization
        using (var verifyDb = new IdentityDbContext(_identityOptions))
        {
            var authService = new AuthorizationService(
                new UserRepository(verifyDb),
                new RoleRepository(verifyDb),
                new PermissionRepository(verifyDb),
                new NullPermissionCache(),
                new AuthorizationPolicyProvider());
            var menuPolicy = new MenuAuthorizationPolicy(authService);

            // Admin & Manager now have Printer Health, Audit Analytics, Refund Reports, and POS Refund
            Assert.True(await menuPolicy.CanViewMenuItemAsync(adminUserId, "printerhealth"));
            Assert.True(await menuPolicy.CanViewMenuItemAsync(adminUserId, "auditanalytics"));
            Assert.True(await menuPolicy.CanViewMenuItemAsync(adminUserId, "taxprofiles"));
            Assert.True(await authService.HasPermissionAsync(adminUserId, "feature.pos.refund"));

            Assert.True(await menuPolicy.CanViewMenuItemAsync(managerUserId, "printerhealth"));
            Assert.True(await menuPolicy.CanViewMenuItemAsync(managerUserId, "auditanalytics"));
            Assert.True(await menuPolicy.CanViewMenuItemAsync(managerUserId, "refundreports"));
            Assert.True(await authService.HasPermissionAsync(managerUserId, "feature.pos.refund"));

            // Cashier remains strictly denied (fail-closed)
            Assert.False(await menuPolicy.CanViewMenuItemAsync(cashierUserId, "printerhealth"));
            Assert.False(await menuPolicy.CanViewMenuItemAsync(cashierUserId, "auditanalytics"));
            Assert.False(await menuPolicy.CanViewMenuItemAsync(cashierUserId, "refundreports"));
            Assert.False(await authService.HasPermissionAsync(cashierUserId, "feature.pos.refund"));

            // Unknown user ID fails closed
            Assert.False(await menuPolicy.CanViewMenuItemAsync(Guid.NewGuid(), "printerhealth"));
            Assert.False(await menuPolicy.CanViewMenuItemAsync(Guid.NewGuid(), "auditanalytics"));
        }
    }

    [Fact]
    public async Task MenuAuthorizationPolicy_ReconcilesAuditAnalyticsLegacyAndCanonicalNames()
    {
        Guid legacyUserId;
        Guid canonicalUserId;

        using (var db = new IdentityDbContext(_identityOptions))
        {
            var legacyPerm = Permission.Create(PermissionCode.Create("menu.audit.analytics"), "Legacy Audit Analytics");
            var canonicalPerm = Permission.Create(PermissionCode.Create("menu.auditanalytics"), "Canonical Audit Analytics");
            await db.Permissions.AddRangeAsync(legacyPerm, canonicalPerm);

            var legacyRole = Role.Create(RoleName.Create("LegacyAuditor"));
            legacyRole.AddPermission(legacyPerm.Id);

            var canonicalRole = Role.Create(RoleName.Create("CanonicalAuditor"));
            canonicalRole.AddPermission(canonicalPerm.Id);

            await db.Roles.AddRangeAsync(legacyRole, canonicalRole);

            var legacyUser = User.Create(Email.Create("leg@test.local"), UserName.Create("legacy_aud"), DisplayName.Create("Legacy Auditor"));
            legacyUser.Activate();
            legacyUser.AssignRole(legacyRole.Id);

            var canonicalUser = User.Create(Email.Create("can@test.local"), UserName.Create("canon_aud"), DisplayName.Create("Canonical Auditor"));
            canonicalUser.Activate();
            canonicalUser.AssignRole(canonicalRole.Id);

            await db.Users.AddRangeAsync(legacyUser, canonicalUser);
            await db.SaveChangesAsync();

            legacyUserId = legacyUser.Id.Value;
            canonicalUserId = canonicalUser.Id.Value;
        }

        using (var verifyDb = new IdentityDbContext(_identityOptions))
        {
            var authService = new AuthorizationService(
                new UserRepository(verifyDb),
                new RoleRepository(verifyDb),
                new PermissionRepository(verifyDb),
                new NullPermissionCache(),
                new AuthorizationPolicyProvider());
            var menuPolicy = new MenuAuthorizationPolicy(authService);

            Assert.True(await menuPolicy.CanViewMenuItemAsync(legacyUserId, "auditanalytics"));
            Assert.True(await menuPolicy.CanViewMenuItemAsync(legacyUserId, "audit.analytics"));

            Assert.True(await menuPolicy.CanViewMenuItemAsync(canonicalUserId, "auditanalytics"));
            Assert.True(await menuPolicy.CanViewMenuItemAsync(canonicalUserId, "audit.analytics"));
        }
    }

    private sealed class NullPermissionCache : IPermissionCache
    {
        public Task<IReadOnlyCollection<string>?> GetAsync(Guid userId, System.Threading.CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<string>?>(null);

        public Task SetAsync(Guid userId, IReadOnlyCollection<string> permissionCodes, System.Threading.CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task InvalidateAsync(Guid userId, System.Threading.CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
