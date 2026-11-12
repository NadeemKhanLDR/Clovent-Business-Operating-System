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
}
