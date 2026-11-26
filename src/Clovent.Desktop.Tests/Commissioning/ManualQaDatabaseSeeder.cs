using Clovent.Authentication.Credentials;
using Clovent.Authentication.Infrastructure.Persistence;
using Clovent.Authentication.Infrastructure.Security;
using Clovent.Desktop.Commissioning.Services;
using Clovent.Identity.Branches;
using Clovent.Identity.Companies;
using Clovent.Identity.Infrastructure.Persistence;
using Clovent.Identity.Roles.ValueObjects;
using Clovent.Identity.Users;
using Clovent.Identity.Users.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Desktop.Tests.Commissioning;

public class ManualQaDatabaseSeeder
{
    private const string QaConnectionString =
        @"Server=(localdb)\MSSQLLocalDB;Database=CBOS_ManualQA_20261111;Integrated Security=True;TrustServerCertificate=True;";

    [Fact]
    public async Task Seed_And_Verify_ManualQa_Database()
    {
        var factory = new CommissioningBootstrapFactory();
        using var provider = factory.CreateBootstrapServiceProvider(QaConnectionString);

        // 1. Provision initial master data hierarchy
        var masterProvisioner = new InitialMasterDataProvisioningService();
        var masterRequest = new InitialMasterDataProvisioningRequest
        {
            OrganizationName = "Clovent Global Enterprises",
            OrganizationCode = "ORG-001",
            CompanyName = "Clovent Flagship Dining Ltd",
            CompanyCode = "COMP-001",
            BranchName = "Metropolitan Branch",
            BranchCode = "BR-001",
            TerminalName = "POS-TERM-01",
            TerminalCode = "T-001",
            HardwareId = "0712-7CA3-8171-7B34"
        };

        var masterResult = await masterProvisioner.ProvisionInitialMasterDataAsync(provider, masterRequest);
        Assert.True(masterResult.Success, $"Master data provisioning failed: {masterResult.Message}");

        // 2. Provision first administrator (admin_acceptance)
        var adminProvisioner = new FirstAdminProvisioningService();
        var adminRequest = new FirstAdminProvisioningRequest
        {
            UserName = "admin_acceptance",
            DisplayName = "Acceptance Admin",
            Email = "admin_acceptance@clovent.local",
            Password = "Password@2026!",
            ConfirmPassword = "Password@2026!",
            CompanyId = masterResult.CompanyId,
            BranchId = masterResult.BranchId
        };

        var (adminSuccess, adminMsg, adminUserId) = await adminProvisioner.ProvisionFirstAdminAsync(provider, adminRequest);
        if (!adminSuccess && !adminMsg.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            Assert.Fail($"Administrator provisioning failed: {adminMsg}");
        }

        // 3. Ensure standard dev admin (admin / Admin123!)
        using (var scope = provider.CreateScope())
        {
            var identityDb = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var authDb = scope.ServiceProvider.GetRequiredService<AuthenticationDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<Clovent.Authentication.Application.IPasswordHasher>();

            var adminUserName = UserName.Create("admin");
            var existingAdmin = await identityDb.Users.FirstOrDefaultAsync(u => u.UserName == adminUserName);
            if (existingAdmin is null)
            {
                var adminRole = await identityDb.Roles.FirstAsync(r => r.Name == RoleName.Create("Administrator"));
                var adminUser = User.Create(
                    Email.Create("admin@clovent.local"),
                    adminUserName,
                    DisplayName.Create("Administrator"));
                adminUser.Activate();
                adminUser.AssignRole(adminRole.Id);
                adminUser.AssignCompany(new CompanyId(masterResult.CompanyId));
                adminUser.AssignBranch(new BranchId(masterResult.BranchId));

                await identityDb.Users.AddAsync(adminUser);
                await identityDb.SaveChangesAsync();

                var now = DateTimeOffset.UtcNow;
                var creds = UserCredentials.Create(adminUser.Id, now);
                var hash = passwordHasher.Hash("Admin123!");
                creds.SetPassword(PasswordHash.Create(hash), now);

                await authDb.UserCredentials.AddAsync(creds);
                await authDb.SaveChangesAsync();
            }
        }

        // 4. Verify both accounts exist in Identity and have valid credentials in Authentication
        using (var verifyScope = provider.CreateScope())
        {
            var identityDb = verifyScope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var authDb = verifyScope.ServiceProvider.GetRequiredService<AuthenticationDbContext>();
            var passwordHasher = verifyScope.ServiceProvider.GetRequiredService<Clovent.Authentication.Application.IPasswordHasher>();

            var accUser = await identityDb.Users.FirstOrDefaultAsync(u => u.UserName == UserName.Create("admin_acceptance"));
            Assert.NotNull(accUser);
            Assert.Equal(UserStatus.Active, accUser!.Status);

            var accCreds = await authDb.UserCredentials.FirstOrDefaultAsync(c => c.UserId == accUser.Id);
            Assert.NotNull(accCreds);
            Assert.NotNull(accCreds!.PasswordHash);
            Assert.True(passwordHasher.Verify("Password@2026!", accCreds.PasswordHash!.Value));

            var stdUser = await identityDb.Users.FirstOrDefaultAsync(u => u.UserName == UserName.Create("admin"));
            Assert.NotNull(stdUser);
            Assert.Equal(UserStatus.Active, stdUser!.Status);

            var stdCreds = await authDb.UserCredentials.FirstOrDefaultAsync(c => c.UserId == stdUser.Id);
            Assert.NotNull(stdCreds);
            Assert.NotNull(stdCreds!.PasswordHash);
            Assert.True(passwordHasher.Verify("Admin123!", stdCreds.PasswordHash!.Value));
        }
    }
}
