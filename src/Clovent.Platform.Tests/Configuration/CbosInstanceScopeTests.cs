using Clovent.Platform.Configuration;
using Clovent.Platform.Logging;
using Clovent.Platform.Printing;
using Clovent.Platform.Tests.TestSupport;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Clovent.Platform.Tests.Configuration;

[Collection("QaScopeSequential")]
public sealed class CbosInstanceScopeTests : IDisposable
{
    private readonly List<string> _nonExistentRootsToClean = [];

    public void Dispose()
    {
        CbosInstanceScope.ResetToProduction();
        foreach (var path in _nonExistentRootsToClean)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, recursive: true);
                }
            }
            catch
            {
            }
        }
    }

    private string AllocateCleanQaRootPath()
    {
        var path = Path.Combine(Path.GetTempPath(), "cbos-qa-test-" + Guid.NewGuid().ToString("N"));
        _nonExistentRootsToClean.Add(path);
        return path;
    }

    [Fact]
    public void InitializeFromStartup_WithoutQaFlagsOrEnv_PreservesProductionModeAndConfiguration()
    {
        using var configDir = new TempDirectory();
        configDir.WriteFile("appsettings.json", """
            {
              "ConnectionStrings": {
                "Default": "Server=.;Database=Clovent_BusinessOperatingSystem;Trusted_Connection=True;"
              },
              "Logging": {
                "File": {
                  "Directory": "C:\\SomeProdLogs"
                }
              }
            }
            """);

        var scope = CbosInstanceScope.InitializeFromStartup([], _ => null);

        Assert.False(scope.IsQaMode);
        Assert.Null(scope.QaInstanceRoot);
        Assert.Null(scope.ExplicitConnectionString);

        var config = PlatformConfiguration.Build(configDir.Path);
        Assert.Equal("Server=.;Database=Clovent_BusinessOperatingSystem;Trusted_Connection=True;", config["ConnectionStrings:Default"]);
        Assert.Equal("C:\\SomeProdLogs", config["Logging:File:Directory"]);
    }

    [Theory]
    [InlineData(new object[] { new[] { "--qa" } })]
    [InlineData(new object[] { new[] { "--qa-mode" } })]
    [InlineData(new object[] { new[] { "--qa-root" } })]
    [InlineData(new object[] { new[] { "--qa-root=" } })]
    [InlineData(new object[] { new[] { "--qa-root", "relative-qa-dir" } })]
    [InlineData(new object[] { new[] { "--qa-root", "C:\\" } })]
    public void InitializeFromStartup_InvalidOrMissingQaRoot_FailsBeforeCreatingDirectories(string[] args)
    {
        var ex = Assert.Throws<InvalidQaConfigurationException>(
            () => CbosInstanceScope.InitializeFromStartup(args, _ => null));

        Assert.NotNull(ex);
        Assert.False(CbosInstanceScope.Current.IsQaMode);
    }

    [Fact]
    public void InitializeFromStartup_QaRootWithoutConnectionTarget_FailsBeforeCreatingRootDirectory()
    {
        var qaRoot = AllocateCleanQaRootPath();
        Assert.False(Directory.Exists(qaRoot));

        // Even if production env ConnectionStrings__Default points to Clovent_BusinessOperatingSystem,
        // QA mode must reject it and refuse to create the QA root.
        var ex = Assert.Throws<InvalidQaConfigurationException>(() =>
            CbosInstanceScope.InitializeFromStartup(
                ["--qa-root", qaRoot],
                name => name == "ConnectionStrings__Default"
                    ? "Server=.;Database=Clovent_BusinessOperatingSystem;Trusted_Connection=True;"
                    : null));

        Assert.Contains("explicit", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(Directory.Exists(qaRoot));
    }

    [Theory]
    [InlineData("Server=.;Database=Clovent_BusinessOperatingSystem;Trusted_Connection=True;")]
    [InlineData("Server=.;Trusted_Connection=True;")]
    [InlineData("Database=CBOS_QA_Only;Trusted_Connection=True;")]
    [InlineData("not a valid === connection string ===")]
    public void InitializeFromStartup_InvalidOrProductionQaConnection_FailsBeforeCreatingRootDirectory(string badConnection)
    {
        var qaRoot = AllocateCleanQaRootPath();
        Assert.False(Directory.Exists(qaRoot));

        Assert.Throws<InvalidQaConfigurationException>(() =>
            CbosInstanceScope.InitializeFromStartup(
                ["--qa-root", qaRoot, "--qa-connection", badConnection],
                _ => null));

        Assert.False(Directory.Exists(qaRoot));
    }

    [Fact]
    public void InitializeFromStartup_QaRootOverlappingSharedCloventFolders_IsRejected()
    {
        var programDataClovent = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Clovent",
            "QA-Overlap");
        var localAppDataClovent = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Clovent",
            "QA-Overlap");

        const string validConn = "Server=(localdb)\\MSSQLLocalDB;Database=CBOS_QA_IsolationTest;Trusted_Connection=True;";

        Assert.Throws<InvalidQaConfigurationException>(() =>
            CbosInstanceScope.InitializeFromStartup(["--qa-root", programDataClovent, "--qa-connection", validConn], _ => null));

        Assert.Throws<InvalidQaConfigurationException>(() =>
            CbosInstanceScope.InitializeFromStartup(["--qa-root", localAppDataClovent, "--qa-connection", validConn], _ => null));
    }

    [Fact]
    public void InitializeFromStartup_ConfigurationPrecedence_CliOverridesEnvironmentAndSavedQaConfig()
    {
        var envRoot = AllocateCleanQaRootPath();
        var cliRoot = AllocateCleanQaRootPath();
        Directory.CreateDirectory(Path.Combine(cliRoot, "Config"));
        File.WriteAllText(
            Path.Combine(cliRoot, "Config", "database.config.json"),
            """{"Server":"(localdb)\\MSSQLLocalDB","Database":"CBOS_QA_Saved"}""");

        var env = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [CbosInstanceScope.QaRootEnvironmentVariable] = envRoot,
            [CbosInstanceScope.QaConnectionEnvironmentVariable] = "Server=(localdb)\\MSSQLLocalDB;Database=CBOS_QA_Env;Trusted_Connection=True;"
        };

        // CLI wins over Env and saved QA file
        var cliWinsScope = CbosInstanceScope.InitializeFromStartup(
            ["--qa-root", cliRoot, "--qa-connection", "Server=(localdb)\\MSSQLLocalDB;Database=CBOS_QA_Cli;Trusted_Connection=True;"],
            name => env.GetValueOrDefault(name));

        Assert.True(cliWinsScope.IsQaMode);
        Assert.Equal(Path.GetFullPath(cliRoot), cliWinsScope.QaInstanceRoot);
        Assert.Equal("CBOS_QA_Cli", cliWinsScope.DatabaseName);

        // Env wins over saved QA file when CLI flags omitted
        Directory.CreateDirectory(Path.Combine(envRoot, "Config"));
        File.WriteAllText(
            Path.Combine(envRoot, "Config", "database.config.json"),
            """{"Server":"(localdb)\\MSSQLLocalDB","Database":"CBOS_QA_SavedEnv"}""");

        var envWinsScope = CbosInstanceScope.InitializeFromStartup([], name => env.GetValueOrDefault(name));
        Assert.Equal(Path.GetFullPath(envRoot), envWinsScope.QaInstanceRoot);
        Assert.Equal("CBOS_QA_Env", envWinsScope.DatabaseName);

        // Saved QA config is accepted for restart without CLI/Env connection
        var savedWinsScope = CbosInstanceScope.InitializeFromStartup(["--qa-root", cliRoot], _ => null);
        Assert.Equal(Path.GetFullPath(cliRoot), savedWinsScope.QaInstanceRoot);
        Assert.Equal("CBOS_QA_Saved", savedWinsScope.DatabaseName);
    }

    [Fact]
    public void PlatformConfiguration_InQaMode_StripsAppsettingsDefaultDbAndRejectsOutOfRootLogs()
    {
        var qaRoot = AllocateCleanQaRootPath();
        const string qaConn = "Server=(localdb)\\MSSQLLocalDB;Database=CBOS_QA_PlatformTest;Trusted_Connection=True;";
        using var scopeHandle = CbosInstanceScope.EnterQaScopeForTesting(qaRoot, qaConn);

        using var badConfigDir = new TempDirectory();
        badConfigDir.WriteFile("appsettings.json", """
            {
              "ConnectionStrings": {
                "Default": "Server=.;Database=Clovent_BusinessOperatingSystem;Trusted_Connection=True;"
              },
              "Logging": {
                "File": {
                  "Directory": "C:\\UnauthorizedExternalLogs"
                }
              }
            }
            """);

        Assert.Throws<InvalidQaConfigurationException>(() => PlatformConfiguration.Build(badConfigDir.Path));

        using var goodConfigDir = new TempDirectory();
        goodConfigDir.WriteFile("appsettings.json", """
            {
              "ConnectionStrings": {
                "Default": "Server=.;Database=Clovent_BusinessOperatingSystem;Trusted_Connection=True;"
              }
            }
            """);

        var cfg = PlatformConfiguration.Build(goodConfigDir.Path);
        Assert.Equal(qaConn, cfg["ConnectionStrings:Default"]);
        Assert.StartsWith(Path.GetFullPath(qaRoot), cfg["Logging:File:Directory"], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PlatformStores_InQaMode_RouteLogsPrintJobsAndQuarantineStrictlyInsideSeparateQaRoots()
    {
        var qaRootA = AllocateCleanQaRootPath();
        var qaRootB = AllocateCleanQaRootPath();
        const string connA = "Server=(localdb)\\MSSQLLocalDB;Database=CBOS_QA_A;Trusted_Connection=True;";
        const string connB = "Server=(localdb)\\MSSQLLocalDB;Database=CBOS_QA_B;Trusted_Connection=True;";

        using (CbosInstanceScope.EnterQaScopeForTesting(qaRootA, connA))
        {
            using var loggerProvider = new FileLoggerProvider(new FileLoggerOptions());
            var logger = loggerProvider.CreateLogger("QaTestA");
            logger.LogInformation("hello from QA instance A");

            var tracker = new DurableFilePrinterJobTracker();
            var quarantine = new PrintJobQuarantineStore();

            Assert.StartsWith(Path.GetFullPath(qaRootA), loggerProvider.LogDirectory, StringComparison.OrdinalIgnoreCase);
            Assert.StartsWith(Path.GetFullPath(qaRootA), tracker.StorageDirectory, StringComparison.OrdinalIgnoreCase);
            Assert.True(Directory.GetFiles(loggerProvider.LogDirectory, "clovent-*.log").Length > 0);
            Assert.NotNull(quarantine);
            Assert.True(Directory.Exists(CbosInstanceScope.Current.PrimaryPrintJobQuarantineDirectory));
        }

        using (CbosInstanceScope.EnterQaScopeForTesting(qaRootB, connB))
        {
            using var loggerProviderB = new FileLoggerProvider(new FileLoggerOptions());
            var trackerB = new DurableFilePrinterJobTracker();

            Assert.StartsWith(Path.GetFullPath(qaRootB), loggerProviderB.LogDirectory, StringComparison.OrdinalIgnoreCase);
            Assert.StartsWith(Path.GetFullPath(qaRootB), trackerB.StorageDirectory, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(qaRootA, loggerProviderB.LogDirectory, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(qaRootA, trackerB.StorageDirectory, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void PlatformStores_InQaMode_RejectExplicitConstructorPathsOutsideQaRootBeforeTouchDisk()
    {
        var qaRoot = AllocateCleanQaRootPath();
        var outsideDir = AllocateCleanQaRootPath();
        const string conn = "Server=(localdb)\\MSSQLLocalDB;Database=CBOS_QA_ExplicitPath;Trusted_Connection=True;";

        using (CbosInstanceScope.EnterQaScopeForTesting(qaRoot, conn))
        {
            Assert.Throws<InvalidQaConfigurationException>(() => new FileLoggerProvider(new FileLoggerOptions
            {
                Directory = outsideDir
            }));
            Assert.Throws<InvalidQaConfigurationException>(() => new DurableFilePrinterJobTracker(outsideDir));
            Assert.Throws<InvalidQaConfigurationException>(() => new PrintJobQuarantineStore(outsideDir));

            Assert.False(Directory.Exists(outsideDir), "Invalid explicit constructor paths must fail before creating directories.");

            // Custom subdirectory inside QA root succeeds cleanly.
            var insideDir = Path.Combine(qaRoot, "CustomSub", "PrintJobs");
            var tracker = new DurableFilePrinterJobTracker(insideDir);
            Assert.StartsWith(Path.GetFullPath(qaRoot), tracker.StorageDirectory, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void PlatformConfiguration_InQaMode_OverridesAllModuleConnectionStringsFromJsonAndEnv()
    {
        var qaRoot = AllocateCleanQaRootPath();
        const string qaConn = "Server=(localdb)\\MSSQLLocalDB;Database=CBOS_QA_AllModules;Trusted_Connection=True;";
        using var configDir = new TempDirectory();
        configDir.WriteFile("appsettings.json", """
            {
              "ConnectionStrings": {
                "Default": "Server=.;Database=Clovent_BusinessOperatingSystem;Trusted_Connection=True;",
                "DefaultConnection": "Server=.;Database=Clovent_BusinessOperatingSystem;Trusted_Connection=True;",
                "Authentication": "Server=.;Database=AuthProd;Trusted_Connection=True;",
                "Catalog": "Server=.;Database=CatalogProd;Trusted_Connection=True;",
                "Identity": "Server=.;Database=IdentityProd;Trusted_Connection=True;",
                "Inventory": "Server=.;Database=InventoryProd;Trusted_Connection=True;",
                "MasterData": "Server=.;Database=MasterDataProd;Trusted_Connection=True;",
                "Restaurant": "Server=.;Database=RestaurantProd;Trusted_Connection=True;"
              }
            }
            """);

        using (CbosInstanceScope.EnterQaScopeForTesting(qaRoot, qaConn))
        {
            var cfg = PlatformConfiguration.Build(configDir.Path);
            string[] keys = ["Default", "DefaultConnection", "Authentication", "Catalog", "Identity", "Inventory", "MasterData", "Restaurant"];
            foreach (var key in keys)
            {
                Assert.Equal(qaConn, cfg[$"ConnectionStrings:{key}"]);
            }
        }
    }

    [Theory]
    [InlineData("Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;")]
    [InlineData("Server=(localdb)\\MSSQLLocalDB;Database=Clovent_BusinessOperatingSystem_Copy;Trusted_Connection=True;")]
    [InlineData("Server=(localdb)\\MSSQLLocalDB;Database=CBOS_QA_Production;Trusted_Connection=True;")]
    [InlineData("Server=(localdb)\\MSSQLLocalDB;Database=CBOS_QA_Live;Trusted_Connection=True;")]
    [InlineData("Server=(localdb)\\MSSQLLocalDB;Database=RandomAppDatabase;Trusted_Connection=True;")]
    [InlineData("Server=.;Database=CBOS_QA_UnapprovedRemote;Trusted_Connection=True;")]
    public void InitializeFromStartup_UnapprovedOrUnsafeTarget_FailsBeforeCreatingDirectories(string badConnection)
    {
        var qaRoot = AllocateCleanQaRootPath();
        Assert.False(Directory.Exists(qaRoot));

        Assert.Throws<InvalidQaConfigurationException>(() =>
            CbosInstanceScope.InitializeFromStartup(
                ["--qa-root", qaRoot, "--qa-connection", badConnection],
                _ => null));

        Assert.False(Directory.Exists(qaRoot));
    }

    [Fact]
    public void InitializeFromStartup_ExplicitTargetApproval_GovernsNonLocalDbAndPinnedTargets()
    {
        var qaRoot = AllocateCleanQaRootPath();
        const string nonLocalDbConn = "Server=localhost;Database=CBOS_QA_ApprovedHost;Trusted_Connection=True;";

        // Approved via CLI
        var cliApproved = CbosInstanceScope.InitializeFromStartup(
            ["--qa-root", qaRoot, "--qa-connection", nonLocalDbConn, "--qa-approved-target", "localhost|CBOS_QA_ApprovedHost"],
            _ => null);
        Assert.True(cliApproved.IsQaMode);
        Assert.Equal("CBOS_QA_ApprovedHost", cliApproved.DatabaseName);

        // Approved via file under <QaRoot>\Config\qa-target.approved.json, rejects mismatching connection
        Directory.CreateDirectory(Path.Combine(qaRoot, "Config"));
        File.WriteAllText(
            Path.Combine(qaRoot, "Config", CbosInstanceScope.QaApprovedTargetFileName),
            """{"Server":"(localdb)\\MSSQLLocalDB","Database":"CBOS_QA_Pinned"}""");

        Assert.Throws<InvalidQaConfigurationException>(() =>
            CbosInstanceScope.InitializeFromStartup(
                ["--qa-root", qaRoot, "--qa-connection", "Server=(localdb)\\MSSQLLocalDB;Database=CBOS_QA_Other;Trusted_Connection=True;"],
                _ => null));

        var fileApproved = CbosInstanceScope.InitializeFromStartup(
            ["--qa-root", qaRoot, "--qa-connection", "Server=(localdb)\\MSSQLLocalDB;Database=CBOS_QA_Pinned;Trusted_Connection=True;"],
            _ => null);
        Assert.Equal("CBOS_QA_Pinned", fileApproved.DatabaseName);
    }

    [Fact]
    public void ReparsePointAndJunctions_EscapingQaRootOrPointingToSharedDirs_AreRejected()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var parentDir = AllocateCleanQaRootPath();
        Directory.CreateDirectory(parentDir);

        // 1. Junction root pointing directly into %LocalAppData%\Clovent\QaEscapeTest (without creating inside LocalAppData if absent? Wait: create & delete or test existing temp? Never write to real Clovent dir -> create link target inside ProgramData? No, mklink /J works even BEFORE target check? Better: test LocalAppData\Clovent\... ONLY against non-Clovent or check if exists; wait - mklink /J accepts non-existent target too? Let's test!).
        var localAppDataCloventSub = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Clovent",
            "QaJunctionProbe_NonExistent");
        var badRootJunction = Path.Combine(parentDir, "bad-root-junction");
        CreateJunction(badRootJunction, localAppDataCloventSub);
        _nonExistentRootsToClean.Add(badRootJunction);

        const string validConn = "Server=(localdb)\\MSSQLLocalDB;Database=CBOS_QA_Junction;Trusted_Connection=True;";
        Assert.Throws<InvalidQaConfigurationException>(() =>
            CbosInstanceScope.InitializeFromStartup(["--qa-root", badRootJunction, "--qa-connection", validConn], _ => null));

        // 2. Valid junction root pointing to safe isolated directory, PLUS internal junction Logs -> outside dir must be rejected!
        var realQaRoot = Path.Combine(parentDir, "real-qa-root");
        var safeRootJunction = Path.Combine(parentDir, "safe-qa-junction");
        var outsideEscapeTarget = Path.Combine(parentDir, "outside-escape-target");
        Directory.CreateDirectory(realQaRoot);
        Directory.CreateDirectory(outsideEscapeTarget);
        CreateJunction(safeRootJunction, realQaRoot);
        CreateJunction(Path.Combine(realQaRoot, "Logs"), outsideEscapeTarget);

        using (CbosInstanceScope.EnterQaScopeForTesting(safeRootJunction, validConn))
        {
            Assert.Throws<InvalidQaConfigurationException>(() =>
                new FileLoggerProvider(new FileLoggerOptions()));
            Assert.Throws<InvalidQaConfigurationException>(() =>
                CbosInstanceScope.Current.EnsurePathInsideQaRoot(Path.Combine(safeRootJunction, "Logs", "nested"), "test"));
        }
    }

    private static void CreateJunction(string linkPath, string targetPath)
    {
        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c mklink /J \"{linkPath}\" \"{targetPath}\"",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        })!;
        process.WaitForExit();
        Assert.True(process.ExitCode == 0, $"Failed to create junction '{linkPath}' -> '{targetPath}': {process.StandardError.ReadToEnd()}");
    }
}
