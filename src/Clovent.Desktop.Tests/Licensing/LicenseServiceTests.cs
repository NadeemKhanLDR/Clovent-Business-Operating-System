using System.Security.Cryptography;
using System.Text;
using Clovent.Desktop.Licensing;
using Xunit;

namespace Clovent.Desktop.Tests.Licensing;

public sealed class LicenseServiceTests
{
    private static string? GetV2PrivateKeyXml()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".clovent", "keys", "clovent_vendor_private_key.xml");
        return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
    }

    private static CloventLicense CreateTestLicense(string keyId = LicenseKeys.ActiveKeyId, string licenseType = "Subscription", int days = 365, string? machineId = null)
    {
        var now = DateTimeOffset.UtcNow;
        var issueDate = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, TimeSpan.Zero);
        var license = new CloventLicense
        {
            KeyId = keyId,
            Product = "Clovent Business Operating System",
            LicenseId = Guid.NewGuid(),
            CustomerName = "Acme Global Test",
            CompanyName = "Acme Operations Ltd",
            LicenseType = licenseType,
            IssueDate = issueDate,
            ValidFrom = issueDate,
            ExpiryDate = issueDate.AddDays(days),
            MaxBranches = 5,
            MaxTerminals = 10,
            MaintenanceExpiry = issueDate.AddDays(365),
            AllowedModules = new List<string> { "POS", "BackOffice", "Inventory", "Catalog", "Reporting" },
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

    [Fact]
    public void MachineFingerprint_ReturnsNonEmptyFormattedFingerprint()
    {
        var machineId = MachineFingerprint.GetCurrentMachineId();

        Assert.NotNull(machineId);
        Assert.Matches(@"^[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}$", machineId);
    }

    [Fact]
    public void LicenseKeys_ActiveAndRevokedKeys_ConfiguredCorrectly()
    {
        Assert.Equal("clovent-2026-v2", LicenseKeys.ActiveKeyId);
        Assert.False(LicenseKeys.IsKeyRevoked(LicenseKeys.ActiveKeyId));
        Assert.NotNull(LicenseKeys.GetPublicKey(LicenseKeys.ActiveKeyId));

        Assert.True(LicenseKeys.IsKeyRevoked("clovent-2026-v1"));
        Assert.NotNull(LicenseKeys.GetPublicKey("clovent-2026-v1"));

        Assert.Null(LicenseKeys.GetPublicKey("non-existent-key"));
        Assert.False(LicenseKeys.IsKeyRevoked("non-existent-key"));
    }

    [Fact]
    public void LicenseService_ValidatesV2SignedLicenseSuccessfully()
    {
        var license = CreateTestLicense();
        if (string.IsNullOrEmpty(license.Signature))
        {
            return; // Skip if vendor private key is not on machine
        }

        var result = LicenseService.ValidateLicense(license);

        Assert.Equal(LicenseStatus.Valid, result.Status);
        Assert.True(result.IsAuthorized);
        Assert.Equal("Acme Global Test", result.License?.CustomerName);
        Assert.True(result.DaysRemaining > 300);
    }

    [Fact]
    public void LicenseService_RejectsRevokedV1KeyId()
    {
        var license = CreateTestLicense(keyId: "clovent-2026-v1");
        license.Signature = "dummy-signature";

        var result = LicenseService.ValidateLicense(license);

        Assert.Equal(LicenseStatus.InvalidSignature, result.Status);
        Assert.False(result.IsAuthorized);
        Assert.Contains("revoked", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LicenseService_DetectsTamperedPayload()
    {
        var license = CreateTestLicense();
        if (string.IsNullOrEmpty(license.Signature))
        {
            return;
        }

        // Tamper with customer name without updating signature
        license.CustomerName = "Tampered Pirate Entity";

        var result = LicenseService.ValidateLicense(license);

        Assert.Equal(LicenseStatus.InvalidSignature, result.Status);
        Assert.False(result.IsAuthorized);
        Assert.Contains("verification failed", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LicenseService_DetectsMachineMismatch()
    {
        var license = CreateTestLicense(machineId: "0000-1111-2222-3333");
        if (string.IsNullOrEmpty(license.Signature))
        {
            return;
        }

        var currentMachine = MachineFingerprint.GetCurrentMachineId();
        if (string.Equals(currentMachine, "0000-1111-2222-3333", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var result = LicenseService.ValidateLicense(license);

        Assert.Equal(LicenseStatus.MachineMismatch, result.Status);
        Assert.False(result.IsAuthorized);
    }

    [Fact]
    public void LicenseService_GracePeriod_DetectedWithin14Days()
    {
        var license = CreateTestLicense(days: -5); // Expired 5 days ago
        if (string.IsNullOrEmpty(license.Signature))
        {
            return;
        }

        var result = LicenseService.ValidateLicense(license);

        Assert.Equal(LicenseStatus.GracePeriod, result.Status);
        Assert.True(result.IsAuthorized);
        Assert.Contains("grace period", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LicenseService_Expired_DetectedAfterGracePeriod()
    {
        var license = CreateTestLicense(days: -20); // Expired 20 days ago
        if (string.IsNullOrEmpty(license.Signature))
        {
            return;
        }

        var result = LicenseService.ValidateLicense(license);

        Assert.Equal(LicenseStatus.Expired, result.Status);
        Assert.False(result.IsAuthorized);
    }

    [Fact]
    public void LicenseService_PerpetualLicense_RemainsValidRegardlessOfExpiryDate()
    {
        var license = CreateTestLicense(licenseType: "Perpetual", days: -30);
        if (string.IsNullOrEmpty(license.Signature))
        {
            return;
        }

        var result = LicenseService.ValidateLicense(license);

        Assert.Equal(LicenseStatus.Valid, result.Status);
        Assert.True(result.IsAuthorized);
        Assert.Contains("Perpetual", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CanCreateTransactions_ReflectsAuthorizedStatuses()
    {
        // Valid & GracePeriod allow transactions; others do not
        Assert.True(new LicenseValidationResult { Status = LicenseStatus.Valid }.IsAuthorized);
        Assert.True(new LicenseValidationResult { Status = LicenseStatus.GracePeriod }.IsAuthorized);
        Assert.False(new LicenseValidationResult { Status = LicenseStatus.Expired }.IsAuthorized);
        Assert.False(new LicenseValidationResult { Status = LicenseStatus.ClockTampered }.IsAuthorized);
        Assert.False(new LicenseValidationResult { Status = LicenseStatus.MachineMismatch }.IsAuthorized);
        Assert.False(new LicenseValidationResult { Status = LicenseStatus.InvalidSignature }.IsAuthorized);
        Assert.False(new LicenseValidationResult { Status = LicenseStatus.Unlicensed }.IsAuthorized);
    }
}
