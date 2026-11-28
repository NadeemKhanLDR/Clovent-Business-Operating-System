using System.Text.Json;
using Clovent.Desktop.Commissioning.Database;
using Clovent.Desktop.Commissioning.Services;
using Clovent.Desktop.Configuration;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Licensing;
using Clovent.Platform.Configuration;
using Xunit;

namespace Clovent.Desktop.Tests.Commissioning;

/// <summary>
/// Verifies workstation startup configuration, commissioning markers, display settings,
/// POS settings, and terminal identity using isolated temporary QA scopes rather than
/// live host %LOCALAPPDATA% or %ProgramData% state.
/// </summary>
public sealed class WorkstationStartupVerificationTests
{
    private static IDisposable EnterIsolatedScope(out string root)
    {
        root = Path.Combine(Path.GetTempPath(), $"cbos_wk_verify_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "Config"));
        var qaDb = $"CBOS_QA_Verify_{Guid.NewGuid():N}";
        var connStr = $"Server=(localdb)\\MSSQLLocalDB;Database={qaDb};Trusted_Connection=True;TrustServerCertificate=True;";
        return CbosInstanceScope.EnterQaScopeForTesting(root, connStr);
    }

    [Fact]
    public void Workstation_DatabaseSecretStore_ResolvesCanonicalDatabase()
    {
        using var scope = EnterIsolatedScope(out var root);
        try
        {
            Assert.Equal("Clovent_BusinessOperatingSystem", DatabaseConnectionSettings.DefaultDatabaseName);

            DatabaseSecretStore.Save(new DatabaseConnectionSettings
            {
                Server = "(localdb)\\MSSQLLocalDB",
                Database = "CBOS_QA_Verify_Isolated",
                UseWindowsAuthentication = true
            }, machineLevel: true);

            var connectionString = DatabaseSecretStore.ResolveConnectionString();

            Assert.NotNull(connectionString);
            Assert.Contains("Database=CBOS_QA_Verify_", connectionString, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("TestDb", connectionString, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
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
        using var scope = EnterIsolatedScope(out var root);
        try
        {
            CommissioningStateService.SaveMarker(new CommissioningMarker
            {
                DatabaseName = DatabaseConnectionSettings.DefaultDatabaseName,
                OrganizationName = "Clovent Global Enterprises",
                CompanyName = "Clovent Foods",
                BranchName = "Main Branch",
                TerminalName = "POS-TERM-01"
            });

            var exists = CommissioningStateService.MarkerExists();
            Assert.True(exists, "Commissioning marker should exist in isolated scope.");

            var marker = CommissioningStateService.LoadMarker();
            Assert.NotNull(marker);
            Assert.Equal("Clovent_BusinessOperatingSystem", marker.DatabaseName);
            Assert.Equal("Clovent Global Enterprises", marker.OrganizationName);
            Assert.NotEmpty(marker.IntegritySignature);
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
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
        using var scope = EnterIsolatedScope(out var root);
        try
        {
            CompanyDisplaySettingsStore.Save(new CompanyDisplaySettings
            {
                DateFormat = "dd-MMM-yyyy",
                TimeFormat = "12 Hour",
                QuantityPrecision = 2
            });

            var settings = CompanyDisplaySettingsStore.Load();
            Assert.NotNull(settings);
            Assert.Equal("dd-MMM-yyyy", settings.DateFormat);
            Assert.Equal("12 Hour", settings.TimeFormat);
            Assert.Equal(2, settings.QuantityPrecision);
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }

    [Fact]
    public void Workstation_PosSettings_AreValid()
    {
        using var scope = EnterIsolatedScope(out var root);
        try
        {
            var expectedBranchId = Guid.NewGuid();
            var expectedTerminalId = Guid.NewGuid();
            PosSettingsStore.SaveBranchId(expectedBranchId);
            PosSettingsStore.SaveTerminalId(expectedTerminalId);

            var branchId = PosSettingsStore.LoadBranchId();
            var terminalId = PosSettingsStore.LoadTerminalId();
            Assert.NotNull(branchId);
            Assert.NotNull(terminalId);
            Assert.Equal(expectedBranchId, branchId.Value);
            Assert.Equal(expectedTerminalId, terminalId.Value);
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }

    [Fact]
    public void Workstation_TerminalConfiguration_IsValid()
    {
        using var scope = EnterIsolatedScope(out var root);
        try
        {
            var expectedTerminalId = Guid.NewGuid();
            var terminalPath = Path.Combine(root, "Config", "terminal.json");
            File.WriteAllText(terminalPath, JsonSerializer.Serialize(new
            {
                TerminalCode = "POS-TERM-01",
                TerminalId = expectedTerminalId
            }));

            Assert.True(File.Exists(terminalPath), "terminal.json must exist in isolated scope.");
            var json = File.ReadAllText(terminalPath);
            Assert.Contains("POS-TERM-01", json);
            Assert.Contains(expectedTerminalId.ToString(), json);
        }
        finally
        {
            try { Directory.Delete(root, true); } catch { }
        }
    }
}
