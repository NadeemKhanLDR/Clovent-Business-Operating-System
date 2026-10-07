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
        var branchId = Clovent.Desktop.Forms.Base.PosSettingsStore.LoadBranchId();
        var terminalId = Clovent.Desktop.Forms.Base.PosSettingsStore.LoadTerminalId();
        Assert.NotNull(branchId);
        Assert.NotNull(terminalId);
        Assert.Equal(Guid.Parse("c631f797-ed35-4250-80bb-0de5a0422beb"), branchId.Value);
        Assert.Equal(Guid.Parse("780932ec-c532-4868-80c3-5e7b6b7f8a42"), terminalId.Value);
    }

    [Fact]
    public void Workstation_TerminalConfiguration_IsValid()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var terminalPath = System.IO.Path.Combine(localAppData, "Clovent", "BusinessOperatingSystem", "Config", "terminal.json");
        Assert.True(System.IO.File.Exists(terminalPath), "terminal.json must exist.");
        var json = System.IO.File.ReadAllText(terminalPath);
        Assert.Contains("POS-TERM-01", json);
        Assert.Contains("780932ec-c532-4868-80c3-5e7b6b7f8a42", json);
    }
}

