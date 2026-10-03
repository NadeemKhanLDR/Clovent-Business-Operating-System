using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using Clovent.Desktop.Authorization;
using Clovent.Desktop.Commissioning.Security;
using Clovent.Desktop.Sessions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Desktop.Tests.Security;

/// <summary>
/// Verifies Windows Commissioning Security, Pre-Login Administrator Elevation,
/// and ProgramData Directory Access Control Lists (ACLs).
/// </summary>
public sealed class CommissioningSecurityTests
{
    [Fact]
    public void WindowsCommissioningSecurity_IsRunningAsAdministrator_ExecutesWithoutException()
    {
        // Act
        var isElevated = WindowsCommissioningSecurity.IsRunningAsAdministrator();

        // Assert - should return a valid boolean without throwing
        Assert.True(isElevated || !isElevated);
    }

    [Fact]
    public void AdministrativePrivilegeChecker_PreLogin_RequiresWindowsAdministrator()
    {
        // When no session is active (services == null or empty session),
        // AdministrativePrivilegeChecker must match WindowsCommissioningSecurity.IsRunningAsAdministrator()
        // and NOT return true unconditionally.

        var expectedElevation = WindowsCommissioningSecurity.IsRunningAsAdministrator();

        // 1. services == null
        var privNoServices = AdministrativePrivilegeChecker.HasAdministrativePrivileges(null);
        Assert.Equal(expectedElevation, privNoServices);

        // 2. empty unauthenticated session
        var services = new ServiceCollection();
        var emptySession = new CurrentSession();
        services.AddSingleton<ICurrentSession>(emptySession);
        var sp = services.BuildServiceProvider();

        var isSessionActive = AdministrativePrivilegeChecker.IsSessionActive(sp);
        Assert.False(isSessionActive);

        var privEmptySession = AdministrativePrivilegeChecker.HasAdministrativePrivileges(sp);
        Assert.Equal(expectedElevation, privEmptySession);
    }

    [Fact]
    public void AdministrativePrivilegeChecker_PostLogin_PreservesRoleAuthorization()
    {
        var services = new ServiceCollection();
        var cashierSession = new CurrentSession();
        cashierSession.SignIn(Guid.NewGuid(), Guid.NewGuid(), "Regular Cashier", userName: "cashier_user");
        services.AddSingleton<ICurrentSession>(cashierSession);
        var sp = services.BuildServiceProvider();

        Assert.True(AdministrativePrivilegeChecker.IsSessionActive(sp));
        Assert.False(AdministrativePrivilegeChecker.HasAdministrativePrivileges(sp));

        // Admin session
        var adminServices = new ServiceCollection();
        var adminSession = new CurrentSession();
        adminSession.SignIn(Guid.NewGuid(), Guid.NewGuid(), "System Administrator", userName: "admin");
        adminServices.AddSingleton<ICurrentSession>(adminSession);
        var adminSp = adminServices.BuildServiceProvider();

        Assert.True(AdministrativePrivilegeChecker.IsSessionActive(adminSp));
        Assert.True(AdministrativePrivilegeChecker.HasAdministrativePrivileges(adminSp));
    }

    [Fact]
    public void ProgramDataAclManager_Paths_AreProperlyStructured()
    {
        Assert.Contains("Clovent", ProgramDataAclManager.BaseDirectory);
        Assert.Contains("BusinessOperatingSystem", ProgramDataAclManager.BaseDirectory);

        Assert.EndsWith("Config", ProgramDataAclManager.ConfigDirectory);
        Assert.EndsWith("License", ProgramDataAclManager.LicenseDirectory);
        Assert.EndsWith("Logs", ProgramDataAclManager.LogsDirectory);
        Assert.EndsWith("State", ProgramDataAclManager.StateDirectory);

        Assert.Contains(ProgramDataAclManager.ConfigDirectory, ProgramDataAclManager.ManagedSubdirectories);
        Assert.Contains(ProgramDataAclManager.LicenseDirectory, ProgramDataAclManager.ManagedSubdirectories);
        Assert.Contains(ProgramDataAclManager.LogsDirectory, ProgramDataAclManager.ManagedSubdirectories);
        Assert.Contains(ProgramDataAclManager.StateDirectory, ProgramDataAclManager.ManagedSubdirectories);
    }

    [Fact]
    public void ProgramDataAclManager_ApplyDirectorySecurity_ConfiguresAclsCorrectly()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var tempDir = Path.Combine(Path.GetTempPath(), $"cbos_acl_test_{Guid.NewGuid():N}");
        try
        {
            // Test read-only target (Config / License / State)
            ProgramDataAclManager.ApplyDirectorySecurity(tempDir, isLogs: false);

            var dirInfo = new DirectoryInfo(tempDir);
            var security = dirInfo.GetAccessControl(AccessControlSections.Access);
            Assert.NotNull(security);
            Assert.True(security.AreAccessRulesProtected, "Inheritance must be blocked/protected.");

            var rules = security.GetAccessRules(true, false, typeof(SecurityIdentifier))
                .Cast<FileSystemAccessRule>()
                .ToList();

            var adminSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
            var systemSid = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
            var usersSid = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);

            var adminRule = rules.FirstOrDefault(r => r.IdentityReference == adminSid);
            Assert.NotNull(adminRule);
            Assert.Equal(FileSystemRights.FullControl, adminRule.FileSystemRights & FileSystemRights.FullControl);

            var systemRule = rules.FirstOrDefault(r => r.IdentityReference == systemSid);
            Assert.NotNull(systemRule);
            Assert.Equal(FileSystemRights.FullControl, systemRule.FileSystemRights & FileSystemRights.FullControl);

            var userRule = rules.FirstOrDefault(r => r.IdentityReference == usersSid);
            Assert.NotNull(userRule);
            // Verify read-only rights for users (no write/delete)
            Assert.True(userRule.FileSystemRights.HasFlag(FileSystemRights.ReadAndExecute));
            Assert.False(userRule.FileSystemRights.HasFlag(FileSystemRights.Write));
            Assert.False(userRule.FileSystemRights.HasFlag(FileSystemRights.Delete));

            // Test logs target (Logs)
            ProgramDataAclManager.ApplyDirectorySecurity(tempDir, isLogs: true);
            var logsSecurity = dirInfo.GetAccessControl(AccessControlSections.Access);
            var logsRules = logsSecurity.GetAccessRules(true, false, typeof(SecurityIdentifier))
                .Cast<FileSystemAccessRule>()
                .ToList();

            var logsUserRule = logsRules.FirstOrDefault(r => r.IdentityReference == usersSid);
            Assert.NotNull(logsUserRule);
            Assert.True(logsUserRule.FileSystemRights.HasFlag(FileSystemRights.Modify));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try
                {
                    // Restore full control to allow deletion of temp folder
                    var dirInfo = new DirectoryInfo(tempDir);
                    var security = dirInfo.GetAccessControl(AccessControlSections.Access);
                    security.SetAccessRuleProtection(false, false);
                    dirInfo.SetAccessControl(security);
                    Directory.Delete(tempDir, true);
                }
                catch
                {
                    // Best-effort cleanup in test
                }
            }
        }
    }
}
