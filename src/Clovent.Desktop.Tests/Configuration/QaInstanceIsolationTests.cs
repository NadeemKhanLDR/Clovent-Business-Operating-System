using Clovent.Desktop.Commissioning.Security;
using Clovent.Desktop.Commissioning.Services;
using Clovent.Desktop.Configuration;
using Clovent.Desktop.Forms.Base;
using Clovent.Desktop.Forms.Base.Appearance;
using Clovent.Desktop.Forms.Base.Localization;
using Clovent.Desktop.Forms.Restaurant.MenuItems;
using Clovent.Desktop.Licensing;
using Clovent.Desktop.Printing;
using Clovent.Desktop.Restaurant.Orders;
using Clovent.Desktop.Restaurant.Services;
using Clovent.Desktop.Restaurant.SmartPos;
using Clovent.Platform.Configuration;
using Clovent.Platform.Printing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Clovent.Desktop.Tests.Configuration;

public sealed class QaInstanceIsolationTests : IDisposable
{
    private readonly List<string> _tempDirs = [];

    public void Dispose()
    {
        ResetAllDesktopTestingState();
        foreach (var dir in _tempDirs)
        {
            try
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, recursive: true);
                }
            }
            catch
            {
            }
        }
    }

    private static void ResetAllDesktopTestingState()
    {
        CbosInstanceScope.ResetToProduction();
        DatabaseSecretStore.ResetTestingOverrides();
        CommissioningStateService.ResetTestingOverrides();
        LicenseService.ResetTestingOverrides();
        LicenseTamperGuard.ResetTestingOverrides();
        TrialStateManager.ResetTestingOverrides();
        CompanyDisplaySettingsStore.ResetTestingOverrides();
        PosSettingsStore.ResetTestingOverrides();
        QueueRushModeStore.ResetCacheForTesting();
    }

    private string CreateCleanTempRoot(string prefix)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{prefix}_{Guid.NewGuid():N}");
        _tempDirs.Add(path);
        return path;
    }

    [Fact]
    public void DatabaseSecretStore_InQaMode_IgnoresSharedOverridesAndProductionAppsettings()
    {
        var sharedMockDir = CreateCleanTempRoot("cbos-shared-mock");
        Directory.CreateDirectory(sharedMockDir);
        var fakeUserConfig = Path.Combine(sharedMockDir, "user-db.json");
        var fakeMachineConfig = Path.Combine(sharedMockDir, "machine-db.json");
        DatabaseSecretStore.SetTestingOverrides(fakeUserConfig, fakeMachineConfig);

        // Seed the mocked shared config files pointing at production DB.
        DatabaseSecretStore.Save(new DatabaseConnectionSettings
        {
            Server = "PROD-SERVER",
            Database = DatabaseConnectionSettings.DefaultDatabaseName,
            UseWindowsAuthentication = true
        });
        Assert.True(File.Exists(fakeUserConfig));

        var qaRoot = CreateCleanTempRoot("cbos-qa-db");
        const string qaConn = "Server=(localdb)\\MSSQLLocalDB;Database=CBOS_QA_DesktopTest;Trusted_Connection=True;TrustServerCertificate=True;";

        var configWithProdDefault = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Server=.;Database=Clovent_BusinessOperatingSystem;Trusted_Connection=True;"
            })
            .Build();

        using (CbosInstanceScope.EnterQaScopeForTesting(qaRoot, qaConn))
        {
            Assert.Equal(qaConn, DatabaseSecretStore.ResolveConnectionString(configWithProdDefault));

            var loaded = DatabaseSecretStore.Load();
            Assert.NotNull(loaded);
            Assert.Equal("(localdb)\\MSSQLLocalDB", loaded.Server);
            Assert.Equal("CBOS_QA_DesktopTest", loaded.Database);

            // Saving inside QA mode writes only to <QaRoot>\Config\database.config.json,
            // never to the production override path, and refuses default production DB.
            Assert.Throws<InvalidQaConfigurationException>(() => DatabaseSecretStore.Save(new DatabaseConnectionSettings
            {
                Server = ".",
                Database = DatabaseConnectionSettings.DefaultDatabaseName
            }));

            DatabaseSecretStore.Save(new DatabaseConnectionSettings
            {
                Server = "(localdb)\\MSSQLLocalDB",
                Database = "CBOS_QA_DesktopSaved",
                UseWindowsAuthentication = true
            }, machineLevel: true);

            var expectedQaConfig = Path.Combine(qaRoot, "Config", "database.config.json");
            Assert.True(File.Exists(expectedQaConfig));
            Assert.Equal(expectedQaConfig, DatabaseSecretStore.GetMachineConfigFilePath());
            Assert.Equal(expectedQaConfig, DatabaseSecretStore.GetUserConfigFilePath());
        }

        // Restarting QA instance from saved QA config without --qa-connection preserves precedence
        // and still never touches production config.
        CbosInstanceScope.InitializeFromStartup(["--qa-root", qaRoot], _ => null);
        Assert.Equal("CBOS_QA_DesktopSaved", DatabaseSecretStore.Load()?.Database);
        Assert.Contains("CBOS_QA_DesktopSaved", DatabaseSecretStore.ResolveConnectionString(configWithProdDefault), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CommissioningLicenseTrialAndAclServices_InQaMode_AreStrictlyIsolatedInsideQaRoot()
    {
        var qaRootA = CreateCleanTempRoot("cbos-qa-a");
        var qaRootB = CreateCleanTempRoot("cbos-qa-b");
        const string connA = "Server=(localdb)\\MSSQLLocalDB;Database=CBOS_QA_StateA;Trusted_Connection=True;";
        const string connB = "Server=(localdb)\\MSSQLLocalDB;Database=CBOS_QA_StateB;Trusted_Connection=True;";

        using (CbosInstanceScope.EnterQaScopeForTesting(qaRootA, connA))
        {
            Assert.True(ProgramDataAclManager.ConfigureDirectorySecurity());
            Assert.StartsWith(Path.GetFullPath(qaRootA), ProgramDataAclManager.BaseDirectory, StringComparison.OrdinalIgnoreCase);
            Assert.True(Directory.Exists(ProgramDataAclManager.ConfigDirectory));
            Assert.True(Directory.Exists(ProgramDataAclManager.LicenseDirectory));
            Assert.True(Directory.Exists(ProgramDataAclManager.PrintJobsDirectory));

            Assert.False(CommissioningStateService.MarkerExists());
            CommissioningStateService.SaveMarker(new CommissioningMarker
            {
                MachineId = "QA-MACHINE-A",
                DatabaseServer = "(localdb)\\MSSQLLocalDB",
                DatabaseName = "CBOS_QA_StateA",
                TerminalName = "QA-TERM-A"
            });
            Assert.True(CommissioningStateService.MarkerExists());
            Assert.True(File.Exists(Path.Combine(qaRootA, "Config", "commissioning.json")));

            Assert.StartsWith(Path.GetFullPath(qaRootA), LicenseService.GetProgramDataLicensePath(), StringComparison.OrdinalIgnoreCase);
            Assert.StartsWith(Path.GetFullPath(qaRootA), LicenseService.GetLocalAppDataLicensePath(), StringComparison.OrdinalIgnoreCase);
            Assert.StartsWith(Path.GetFullPath(qaRootA), LicenseTamperGuard.GetGuardFilePath(), StringComparison.OrdinalIgnoreCase);
            Assert.StartsWith(Path.GetFullPath(qaRootA), TrialStateManager.GetPrimaryTrialStatePath(), StringComparison.OrdinalIgnoreCase);

            LicenseTamperGuard.VerifyAndUpdateClock(out _);
            Assert.True(File.Exists(Path.Combine(qaRootA, "State", "license_guard.dat")));
        }

        // A second independent QA instance sees empty clean state and cannot see QA-A's marker.
        using (CbosInstanceScope.EnterQaScopeForTesting(qaRootB, connB))
        {
            Assert.False(CommissioningStateService.MarkerExists());
            Assert.Null(CommissioningStateService.LoadMarker());
            Assert.StartsWith(Path.GetFullPath(qaRootB), LicenseService.GetProgramDataLicensePath(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(qaRootA, LicenseService.GetProgramDataLicensePath(), StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task DesktopOperationalStoresAndPreferences_RouteStrictlyInsideEachQaRoot()
    {
        var qaRootA = CreateCleanTempRoot("cbos-qa-pref-a");
        var qaRootB = CreateCleanTempRoot("cbos-qa-pref-b");
        const string connA = "Server=(localdb)\\MSSQLLocalDB;Database=CBOS_QA_PrefA;Trusted_Connection=True;";
        const string connB = "Server=(localdb)\\MSSQLLocalDB;Database=CBOS_QA_PrefB;Trusted_Connection=True;";

        var checkpointId = Guid.NewGuid();
        var methodId = Guid.NewGuid();

        using (CbosInstanceScope.EnterQaScopeForTesting(qaRootA, connA))
        {
            var printerStore = new WindowsPrinterConfigurationStore();
            await printerStore.SaveAsync(new PrinterConfiguration { DefaultReceiptProfileId = checkpointId });
            Assert.True(File.Exists(Path.Combine(qaRootA, "Config", "printers.json")));

            var checkpointStore = new ActiveOrderCheckpointStore();
            await checkpointStore.SaveCheckpointAsync(new CartCheckpoint(
                checkpointId, "TERM-A", null, "QA", "DineIn", null, null, DateTimeOffset.UtcNow, [], null, null));
            Assert.True(File.Exists(Path.Combine(qaRootA, "CartCheckpoints", "terminal_TERM-A.json")));

            LanguagePreferenceStore.Save("ur");
            AppearanceSettingsStore.Save([]);
            CompanyDisplaySettingsStore.Save(new CompanyDisplaySettings { TimeFormat = "24 Hour" });
            PosSettingsStore.SaveItemsPerRow(6);
            PosPaymentMethodPreferenceStore.Save(methodId);
            QueueRushModeStore.ResetCacheForTesting();
            QueueRushModeStore.SetEnabled(true);

            Assert.True(File.Exists(Path.Combine(qaRootA, "User", "language.txt")));
            Assert.True(File.Exists(Path.Combine(qaRootA, "User", "appearance.json")));
            Assert.True(File.Exists(Path.Combine(qaRootA, "User", "company_display_settings.json")));
            Assert.True(File.Exists(Path.Combine(qaRootA, "User", "pos_settings.json")));
            Assert.True(File.Exists(Path.Combine(qaRootA, "User", "pos-payment-method.txt")));
            Assert.True(File.Exists(Path.Combine(qaRootA, "User", "queue-rush-mode.txt")));
            Assert.StartsWith(Path.GetFullPath(qaRootA), SmartPosLayoutTelemetry.TelemetryDir, StringComparison.OrdinalIgnoreCase);
            Assert.StartsWith(Path.GetFullPath(qaRootA), MenuItemImageStore.GetPath(checkpointId), StringComparison.OrdinalIgnoreCase);
        }

        // Switch to QA instance B and reset in-memory caches: none of QA-A's preferences leak into QA-B.
        CompanyDisplaySettingsStore.ResetTestingOverrides();
        PosSettingsStore.ResetTestingOverrides();
        QueueRushModeStore.ResetCacheForTesting();

        using (CbosInstanceScope.EnterQaScopeForTesting(qaRootB, connB))
        {
            var printerStoreB = new WindowsPrinterConfigurationStore();
            var cfgB = await printerStoreB.LoadAsync();
            Assert.NotEqual(checkpointId, cfgB.DefaultReceiptProfileId);

            var checkpointStoreB = new ActiveOrderCheckpointStore();
            Assert.Null(await checkpointStoreB.LoadCheckpointAsync("TERM-A"));

            Assert.Equal(LanguagePreferenceStore.DefaultCultureCode, LanguagePreferenceStore.Load());
            Assert.Null(PosPaymentMethodPreferenceStore.Load());
            Assert.False(QueueRushModeStore.IsEnabled());
            Assert.StartsWith(Path.GetFullPath(qaRootB), SmartPosLayoutTelemetry.TelemetryDir, StringComparison.OrdinalIgnoreCase);
        }
    }
}
