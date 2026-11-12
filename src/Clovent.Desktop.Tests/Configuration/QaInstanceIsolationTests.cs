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

    [Fact]
    public void DesktopStores_InQaMode_RejectExplicitPathsOutsideQaRootAndUnapprovedTargetSaves()
    {
        var qaRoot = CreateCleanTempRoot("cbos-qa-desktop-escape");
        var outsideDir = CreateCleanTempRoot("cbos-qa-desktop-outside");
        const string conn = "Server=(localdb)\\MSSQLLocalDB;Database=CBOS_QA_DesktopEscape;Trusted_Connection=True;";

        using (CbosInstanceScope.EnterQaScopeForTesting(qaRoot, conn, approvedTarget: "(localdb)\\MSSQLLocalDB|CBOS_QA_DesktopEscape"))
        {
            Assert.Throws<InvalidQaConfigurationException>(() =>
                new WindowsPrinterConfigurationStore(Path.Combine(outsideDir, "printers.json")));
            Assert.Throws<InvalidQaConfigurationException>(() =>
                new ActiveOrderCheckpointStore(outsideDir));
            Assert.Throws<InvalidQaConfigurationException>(() =>
                new ProtectedPaymentAttemptJournal(outsideDir, "TERM-1", () => Guid.NewGuid()));

            Assert.False(Directory.Exists(outsideDir));

            // Saving a different target when pinned via approved target must throw before writing.
            Assert.Throws<InvalidQaConfigurationException>(() =>
                DatabaseSecretStore.Save(new DatabaseConnectionSettings
                {
                    Server = "(localdb)\\MSSQLLocalDB",
                    Database = "CBOS_QA_WrongTarget"
                }));
            Assert.False(File.Exists(Path.Combine(qaRoot, "Config", "database.config.json")));
        }
    }

    [Fact]
    public void LicensingAndTrial_InQaMode_EnforceEntitlementsAndDoNotMutateNonQaOverrides()
    {
        var nonQaMockDir = CreateCleanTempRoot("cbos-nonqa-lic-mock");
        Directory.CreateDirectory(nonQaMockDir);
        var nonQaLicenseFile = Path.Combine(nonQaMockDir, "nonqa.lic");
        var nonQaGuardFile = Path.Combine(nonQaMockDir, "nonqa_guard.dat");
        var nonQaMarkerFile = Path.Combine(nonQaMockDir, "nonqa_commissioning.json");

        // Configure test overrides pointing OUTSIDE any QA root (simulating non-QA host files).
        LicenseService.SetLicenseFilePathForTesting(nonQaLicenseFile);
        LicenseTamperGuard.SetTestingOverrides(nonQaGuardFile);
        CommissioningStateService.SetTestingOverrides(nonQaMarkerFile, nonQaMarkerFile);

        var qaRootA = CreateCleanTempRoot("cbos-qa-lic-a");
        var qaRootB = CreateCleanTempRoot("cbos-qa-lic-b");
        const string connA = "Server=(localdb)\\MSSQLLocalDB;Database=CBOS_QA_LicA;Trusted_Connection=True;";
        const string connB = "Server=(localdb)\\MSSQLLocalDB;Database=CBOS_QA_LicB;Trusted_Connection=True;";

        // Write a malformed/tampered license only inside QA root A -> must fail signature verification & block transactions.
        Directory.CreateDirectory(Path.Combine(qaRootA, "License"));
        File.WriteAllText(Path.Combine(qaRootA, "License", "clovent.lic"), """{"Product":"Clovent Business Operating System","KeyId":"bad","Signature":"invalid"}""");

        using (CbosInstanceScope.EnterQaScopeForTesting(qaRootA, connA))
        {
            var resA = LicenseService.CurrentResult;
            Assert.Equal(LicenseStatus.InvalidSignature, resA.Status);
            Assert.False(LicenseService.CanCreateTransactions());
        }

        // Entering clean QA root B automatically invalidates cached scope result and evaluates clean active trial inside QA root B.
        var fixedNow = new DateTimeOffset(2026, 11, 12, 10, 0, 0, TimeSpan.Zero);
        TrialStateManager.SetTestingOverrides(
            storageDirectory: null,
            timeProvider: () => fixedNow,
            machineId: "QA-MACHINE-LIC-B");

        using (CbosInstanceScope.EnterQaScopeForTesting(qaRootB, connB))
        {
            var trialB = TrialStateManager.StartTrial(startedAtUtc: fixedNow);
            Assert.Equal(TrialStateStatus.Active, trialB.Status);
            Assert.True(File.Exists(Path.Combine(qaRootB, "License", "trial.state")));

            var activeResB = LicenseService.CurrentResult;
            Assert.Equal(LicenseStatus.Valid, activeResB.Status);
            Assert.True(LicenseService.CanCreateTransactions());

            // Advance simulated clock past 30-day evaluation window: QA mode enforces expiration.
            TrialStateManager.SetTestingOverrides(
                storageDirectory: null,
                timeProvider: () => fixedNow.AddDays(31),
                machineId: "QA-MACHINE-LIC-B");
            var expiredResB = LicenseService.Refresh();
            Assert.Equal(LicenseStatus.Expired, expiredResB.Status);
            Assert.False(LicenseService.CanCreateTransactions());
        }

        // Host/non-QA mock files were never touched by QA operations.
        Assert.False(File.Exists(nonQaLicenseFile));
        Assert.False(File.Exists(nonQaGuardFile));
        Assert.False(File.Exists(nonQaMarkerFile));
    }
}
