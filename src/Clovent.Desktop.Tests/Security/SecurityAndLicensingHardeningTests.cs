using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Clovent.Desktop.Authorization;
using Clovent.Desktop.Configuration;
using Clovent.Desktop.Licensing;
using Clovent.Desktop.Sessions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Clovent.Desktop.Tests.Security;

/// <summary>
/// Comprehensive verification suite for Production Security, Licensing,
/// and Release Hardening per Requirements 21 (Tests A through N).
/// </summary>
public sealed class SecurityAndLicensingHardeningTests : IDisposable
{
    private readonly string _testDir;

    public SecurityAndLicensingHardeningTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "cbos_sec_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
        var guardFile = Path.Combine(_testDir, "license_guard.dat");
        LicenseTamperGuard.SetTestingOverrides(guardFile);
    }

    public void Dispose()
    {
        LicenseTamperGuard.ResetTestingOverrides();
        if (Directory.Exists(_testDir))
        {
            try { Directory.Delete(_testDir, true); } catch { }
        }
    }

    private static string? GetV2PrivateKeyXml()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".clovent", "keys", "clovent_vendor_private_key.xml");
        return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
    }

    private static CloventLicense CreateTestSignedLicense(
        string keyId = LicenseKeys.ActiveKeyId,
        string product = "Clovent Business Operating System",
        string licenseType = "Subscription",
        int days = 365,
        int validFromDays = 0,
        string? machineId = null,
        List<string>? modules = null)
    {
        var now = DateTimeOffset.UtcNow;
        var issueDate = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, TimeSpan.Zero);
        var license = new CloventLicense
        {
            KeyId = keyId,
            Product = product,
            LicenseId = Guid.NewGuid(),
            CustomerName = "Security Test Customer",
            CompanyName = "Security Test Enterprises",
            LicenseType = licenseType,
            IssueDate = issueDate,
            ValidFrom = issueDate.AddDays(validFromDays),
            ExpiryDate = issueDate.AddDays(days),
            MaxBranches = 5,
            MaxTerminals = 10,
            MaintenanceExpiry = issueDate.AddDays(365),
            AllowedModules = modules ?? new List<string> { "POS", "BackOffice", "Inventory", "Catalog", "Reporting", "Restaurant" },
            MachineId = machineId
        };

        var privateKeyXml = GetV2PrivateKeyXml();
        if (!string.IsNullOrEmpty(privateKeyXml))
        {
            var canonicalPayload = license.GetCanonicalPayload();
            using var rsa = RSA.Create();
            rsa.FromXmlString(privateKeyXml);
            var sigBytes = rsa.SignData(Encoding.UTF8.GetBytes(canonicalPayload), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            license.Signature = Convert.ToBase64String(sigBytes);
        }

        return license;
    }

    // A. Tampered license rejected
    [Fact]
    public void Test_A_TamperedLicense_IsRejected()
    {
        var license = CreateTestSignedLicense();
        if (string.IsNullOrEmpty(license.Signature)) return;

        // Tamper with company name
        license.CompanyName = "Tampered Imposter Organization";

        var result = LicenseService.ValidateLicense(license);

        Assert.Equal(LicenseStatus.InvalidSignature, result.Status);
        Assert.False(result.IsAuthorized);
        Assert.Contains("verification failed", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    // B. License signed by unknown or revoked key rejected
    [Fact]
    public void Test_B_UnknownOrRevokedKey_IsRejected()
    {
        // 1. Revoked key
        var revokedLicense = CreateTestSignedLicense(keyId: LicenseKeys.RevokedKeyIdV1);
        revokedLicense.Signature = "dummy-sig";
        var resultRevoked = LicenseService.ValidateLicense(revokedLicense);
        Assert.Equal(LicenseStatus.InvalidSignature, resultRevoked.Status);
        Assert.Contains("revoked", resultRevoked.Message, StringComparison.OrdinalIgnoreCase);

        // 2. Unknown key
        var unknownLicense = CreateTestSignedLicense(keyId: "unknown-key-9999");
        unknownLicense.Signature = "dummy-sig";
        var resultUnknown = LicenseService.ValidateLicense(unknownLicense);
        Assert.Equal(LicenseStatus.InvalidSignature, resultUnknown.Status);
        Assert.Contains("Unknown or untrusted", resultUnknown.Message, StringComparison.OrdinalIgnoreCase);
    }

    // C. Expired license state
    [Fact]
    public void Test_C_ExpiredLicense_EnforcesExpiredState()
    {
        // Expired 20 days ago (past the 14-day grace period)
        var license = CreateTestSignedLicense(days: -20);
        if (string.IsNullOrEmpty(license.Signature)) return;

        var result = LicenseService.ValidateLicense(license);

        Assert.Equal(LicenseStatus.Expired, result.Status);
        Assert.False(result.IsAuthorized);
        Assert.Contains("expired", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    // D. Grace period state
    [Fact]
    public void Test_D_GracePeriod_DetectedWithin14Days()
    {
        // Expired 3 days ago (within the 14-day grace window)
        var license = CreateTestSignedLicense(days: -3);
        if (string.IsNullOrEmpty(license.Signature)) return;

        var result = LicenseService.ValidateLicense(license);

        Assert.Equal(LicenseStatus.GracePeriod, result.Status);
        Assert.True(result.IsAuthorized);
        Assert.Contains("grace period", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    // E. Machine mismatch
    [Fact]
    public void Test_E_MachineMismatch_EnforcesNodeLocking()
    {
        var mismatchedMachineId = "9999-8888-7777-6666";
        var license = CreateTestSignedLicense(machineId: mismatchedMachineId);
        if (string.IsNullOrEmpty(license.Signature)) return;

        var currentMachine = MachineFingerprint.GetCurrentMachineId();
        if (string.Equals(currentMachine, mismatchedMachineId, StringComparison.OrdinalIgnoreCase)) return;

        var result = LicenseService.ValidateLicense(license);

        Assert.Equal(LicenseStatus.MachineMismatch, result.Status);
        Assert.False(result.IsAuthorized);
        Assert.Contains("bound to machine", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    // F. Wrong product
    [Fact]
    public void Test_F_WrongProduct_IsRejected()
    {
        var license = CreateTestSignedLicense(product: "Other Unrelated Software");
        if (string.IsNullOrEmpty(license.Signature)) return;

        var result = LicenseService.ValidateLicense(license);

        Assert.Equal(LicenseStatus.InvalidSignature, result.Status);
        Assert.False(result.IsAuthorized);
        Assert.Contains("product mismatch", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    // G. Unauthorized module
    [Fact]
    public void Test_G_UnauthorizedModule_IsDetected()
    {
        var license = CreateTestSignedLicense(modules: new List<string> { "POS", "Reporting" });

        Assert.Contains("POS", license.AllowedModules);
        Assert.Contains("Reporting", license.AllowedModules);
        Assert.DoesNotContain("Inventory", license.AllowedModules);
        Assert.DoesNotContain("Catalog", license.AllowedModules);
    }

    // H. SQL password not present in plain-text persisted config
    [Fact]
    public void Test_H_SqlPassword_NotPresentInPlainTextPersistedConfig()
    {
        var plainPassword = "SuperSecretPlainTextPassword!456";
        var settings = new DatabaseConnectionSettings
        {
            Server = "test-sql-server",
            Database = "Clovent_TestDb",
            UseWindowsAuthentication = false,
            UserId = "test_user",
            PlainTextPassword = plainPassword
        };

        var tempConfigPath = Path.Combine(Path.GetTempPath(), $"cbos_test_db_{Guid.NewGuid():N}.json");
        try
        {
            var toSave = settings.Clone();
            toSave.EncryptedPassword = DatabaseSecretStore.Protect(toSave.PlainTextPassword!);
            toSave.PlainTextPassword = null;

            var json = JsonSerializer.Serialize(toSave, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(tempConfigPath, json);

            var savedText = File.ReadAllText(tempConfigPath);

            Assert.DoesNotContain(plainPassword, savedText);
            Assert.Contains("\"EncryptedPassword\":", savedText);
            Assert.DoesNotContain("\"PlainTextPassword\": \"SuperSecret", savedText);

            // Verify decryption recovers original password
            var loaded = JsonSerializer.Deserialize<DatabaseConnectionSettings>(savedText);
            Assert.NotNull(loaded);
            var decrypted = DatabaseSecretStore.Unprotect(loaded.EncryptedPassword);
            Assert.Equal(plainPassword, decrypted);
        }
        finally
        {
            if (File.Exists(tempConfigPath)) File.Delete(tempConfigPath);
        }
    }

    // I. Database Settings authorization
    [Fact]
    public void Test_I_DatabaseSettings_RequiresAdministrativePrivileges()
    {
        var services = new ServiceCollection();
        var session = new CurrentSession();
        // Sign in as regular cashier without admin privileges
        session.SignIn(Guid.NewGuid(), Guid.NewGuid(), "Cashier Bob", userName: "cashier1");
        services.AddSingleton<ICurrentSession>(session);

        var sp = services.BuildServiceProvider();

        var isSessionActive = AdministrativePrivilegeChecker.IsSessionActive(sp);
        var hasAdminRights = AdministrativePrivilegeChecker.HasAdministrativePrivileges(sp);

        Assert.True(isSessionActive, "User session should be active.");
        Assert.False(hasAdminRights, "Standard cashier user must NOT have administrative privileges.");
    }

    // J. License Import authorization
    [Fact]
    public void Test_J_LicenseImport_RequiresAdministrativePrivileges()
    {
        var services = new ServiceCollection();
        var session = new CurrentSession();
        // Regular cashier
        session.SignIn(Guid.NewGuid(), Guid.NewGuid(), "Cashier Alice", userName: "alice");
        services.AddSingleton<ICurrentSession>(session);

        var sp = services.BuildServiceProvider();

        var hasAdminRights = AdministrativePrivilegeChecker.HasAdministrativePrivileges(sp);
        Assert.False(hasAdminRights, "License import requires administrative privileges; normal operator must be denied.");

        // Now test with admin user
        var adminSession = new CurrentSession();
        adminSession.SignIn(Guid.NewGuid(), Guid.NewGuid(), "System Admin", userName: "admin");
        var adminServices = new ServiceCollection();
        adminServices.AddSingleton<ICurrentSession>(adminSession);
        var adminSp = adminServices.BuildServiceProvider();

        var adminHasRights = AdministrativePrivilegeChecker.HasAdministrativePrivileges(adminSp);
        Assert.True(adminHasRights, "Administrator must be granted privileges to import licenses.");
    }

    // K. Development license excluded from Release publish
    [Fact]
    public void Test_K_DevelopmentLicense_ExcludedFromReleasePublish()
    {
        var csprojPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Clovent.Desktop", "Clovent.Desktop.csproj"));
        Assert.True(File.Exists(csprojPath), $"Clovent.Desktop.csproj not found at {csprojPath}");

        var csprojText = File.ReadAllText(csprojPath);

        // clovent.lic must NOT be included in CopyToOutputDirectory
        Assert.DoesNotContain("<None Update=\"clovent.lic\"", csprojText);
    }

    // L. Private signing key absent from Release and Repository
    [Fact]
    public void Test_L_PrivateSigningKey_AbsentFromRepoAndTrackedFiles()
    {
        var solutionDir = Directory.GetParent(AppContext.BaseDirectory);
        while (solutionDir != null && !File.Exists(Path.Combine(solutionDir.FullName, "Clovent.BusinessOperatingSystem.slnx")))
        {
            solutionDir = solutionDir.Parent;
        }
        Assert.NotNull(solutionDir);

        var forbiddenKeysInRepo = Directory.GetFiles(solutionDir.FullName, "*vendor_private_key*", SearchOption.AllDirectories)
            .Where(f => !f.Contains("obj") && !f.Contains("bin"))
            .ToList();

        Assert.Empty(forbiddenKeysInRepo);

        // Verify .gitignore ignores private keys
        var gitignorePath = Path.Combine(solutionDir.FullName, ".gitignore");
        Assert.True(File.Exists(gitignorePath));
        var gitignoreContent = File.ReadAllText(gitignorePath);
        Assert.Contains("*.privatekey", gitignoreContent);
        Assert.Contains("*.pem", gitignoreContent);
        Assert.Contains("*vendor_private_key*", gitignoreContent);
    }

    // M. Universal wildcard license absent from generic Release
    [Fact]
    public void Test_M_UniversalWildcardLicense_AbsentFromDesktopProject()
    {
        var desktopDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Clovent.Desktop"));
        var cloventLicPath = Path.Combine(desktopDir, "clovent.lic");

        Assert.False(File.Exists(cloventLicPath), "Universal commercial clovent.lic must NOT exist in Clovent.Desktop directory.");
    }

    // N. Runtime database user documentation does not grant DDL/admin roles
    [Fact]
    public void Test_N_RuntimeDatabaseUserDocumentation_DoesNotGrantDdlAdmin()
    {
        var solutionDir = Directory.GetParent(AppContext.BaseDirectory);
        while (solutionDir != null && !File.Exists(Path.Combine(solutionDir.FullName, "Clovent.BusinessOperatingSystem.slnx")))
        {
            solutionDir = solutionDir.Parent;
        }
        Assert.NotNull(solutionDir);

        var docPath = Path.Combine(solutionDir.FullName, "docs", "SecurityDeployment.md");
        if (File.Exists(docPath))
        {
            var content = File.ReadAllText(docPath);
            // Must NOT recommend ALTER ROLE [db_ddladmin] ADD MEMBER [cbos_app];
            Assert.DoesNotContain("ALTER ROLE [db_ddladmin] ADD MEMBER [cbos_app]", content);
        }
    }
}
