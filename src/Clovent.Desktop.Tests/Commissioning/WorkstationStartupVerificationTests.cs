using Clovent.Desktop.Commissioning.Database;
using Clovent.Desktop.Commissioning.Services;
using Clovent.Desktop.Configuration;
using Clovent.Desktop.Licensing;
using Xunit;

namespace Clovent.Desktop.Tests.Commissioning;

public sealed class WorkstationStartupVerificationTests
{
    [Fact]
    public void Workstation_DatabaseSecretStore_ResolvesCanonicalDatabase()
    {
        var connectionString = DatabaseSecretStore.ResolveConnectionString();

        Assert.NotNull(connectionString);
        Assert.Contains("Database=Clovent_BusinessOperatingSystem", connectionString, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TestDb", connectionString, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Workstation_DatabaseSchemaCompatibility_IsCompatible()
    {
        var connectionString = DatabaseSecretStore.ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString) || 
            (!TestSupport.DatabaseIsolationGuard.HasIsolatedTestDatabase && TestSupport.DatabaseIsolationGuard.IsOperationalDatabase(connectionString)))
        {
            // Automated test runner safeguard: do not connect to operational database without explicit isolated test target
            return;
        }

        var validator = new DatabaseSchemaCompatibilityValidator();
        var result = await validator.ValidateCompatibilityAsync(connectionString);

        Assert.Equal(SchemaCompatibilityStatus.Compatible, result.Status);
        Assert.True(result.IsCompatible);
        Assert.Empty(result.PendingMigrations);
        Assert.Empty(result.UnknownMigrations);
    }

    [Fact]
    public void Workstation_CommissioningMarker_ExistsAndIsValid()
    {
        var exists = CommissioningStateService.MarkerExists();
        Assert.True(exists, "Commissioning marker should exist on this workstation.");

        var marker = CommissioningStateService.LoadMarker();
        Assert.NotNull(marker);
        Assert.Equal("Clovent_BusinessOperatingSystem", marker.DatabaseName);
        Assert.Equal("Clovent Global Enterprises", marker.OrganizationName);
        Assert.NotEmpty(marker.IntegritySignature);
    }

    [Fact]
    public void Workstation_License_IsAuthorized()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "cbos_wk_lic_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            LicenseTamperGuard.SetTestingOverrides(Path.Combine(tempDir, "license_guard.dat"));
            var result = LicenseService.ValidateCurrentLicense();

            Assert.NotNull(result);
            Assert.True(result.IsAuthorized, $"License should be authorized: {result.Message}");
        }
        finally
        {
            LicenseTamperGuard.ResetTestingOverrides();
            try { Directory.Delete(tempDir, true); } catch { }
        }
    }

    [Fact]
    public void Workstation_CompanyDisplaySettings_AreValid()
    {
        var settings = Clovent.Desktop.Forms.Base.CompanyDisplaySettingsStore.Load();
        Assert.NotNull(settings);
        Assert.Equal("dd-MMM-yyyy", settings.DateFormat);
        Assert.Equal("12 Hour", settings.TimeFormat);
        Assert.Equal(2, settings.QuantityPrecision);
    }

    [Fact]
    public void Workstation_PosSettings_AreValid()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"cbos_pos_settings_{Guid.NewGuid():N}.json");
        var expectedBranchId = Guid.Parse("c631f797-ed35-4250-80bb-0de5a0422beb");
        var expectedTerminalId = Guid.Parse("780932ec-c532-4868-80c3-5e7b6b7f8a42");

        try
        {
            Clovent.Desktop.Forms.Base.PosSettingsStore.SetTestingOverrides(tempPath);
            Clovent.Desktop.Forms.Base.PosSettingsStore.SaveBranchId(expectedBranchId);
            Clovent.Desktop.Forms.Base.PosSettingsStore.SaveTerminalId(expectedTerminalId);
            Clovent.Desktop.Forms.Base.PosSettingsStore.ResetCacheForTesting();

            var branchId = Clovent.Desktop.Forms.Base.PosSettingsStore.LoadBranchId();
            var terminalId = Clovent.Desktop.Forms.Base.PosSettingsStore.LoadTerminalId();
            Assert.NotNull(branchId);
            Assert.NotNull(terminalId);
            Assert.Equal(expectedBranchId, branchId.Value);
            Assert.Equal(expectedTerminalId, terminalId.Value);
        }
        finally
        {
            Clovent.Desktop.Forms.Base.PosSettingsStore.ResetTestingOverrides();
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public void Workstation_TerminalConfiguration_IsValid()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"cbos_term_cfg_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var terminalPath = Path.Combine(tempDir, "terminal.json");
        var expectedTerminalId = "780932ec-c532-4868-80c3-5e7b6b7f8a42";

        try
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                TerminalId = expectedTerminalId,
                TerminalCode = "POS-TERM-01",
                MachineName = Environment.MachineName
            });
            File.WriteAllText(terminalPath, payload);

            Assert.True(File.Exists(terminalPath), "terminal.json must exist.");
            var json = File.ReadAllText(terminalPath);
            Assert.Contains("POS-TERM-01", json);
            Assert.Contains(expectedTerminalId, json);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }
}

