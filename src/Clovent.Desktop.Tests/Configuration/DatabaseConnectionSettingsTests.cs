using Clovent.Desktop.Configuration;
using Xunit;

namespace Clovent.Desktop.Tests.Configuration;

public sealed class DatabaseConnectionSettingsTests
{
    [Fact]
    public void BuildConnectionString_WindowsAuthentication_IncludesTrustedConnection()
    {
        var settings = new DatabaseConnectionSettings
        {
            Server = "sql.corp.local",
            Database = "Clovent_BusinessOperatingSystem",
            UseWindowsAuthentication = true
        };

        var connStr = settings.BuildConnectionString();

        Assert.Contains("Server=sql.corp.local;", connStr);
        Assert.Contains("Database=Clovent_BusinessOperatingSystem;", connStr);
        Assert.Contains("Trusted_Connection=True;", connStr);
        Assert.DoesNotContain("User Id=", connStr);
        Assert.DoesNotContain("Password=", connStr);
    }

    [Fact]
    public void BuildConnectionString_SqlAuthentication_IncludesUserIdAndPassword()
    {
        var settings = new DatabaseConnectionSettings
        {
            Server = "192.168.1.50",
            Database = "Clovent_BusinessOperatingSystem",
            UseWindowsAuthentication = false,
            UserId = "cbos_app",
            PlainTextPassword = "SecretAppPassword123!"
        };

        var connStr = settings.BuildConnectionString();

        Assert.Contains("Server=192.168.1.50;", connStr);
        Assert.Contains("Database=Clovent_BusinessOperatingSystem;", connStr);
        Assert.Contains("User Id=cbos_app;", connStr);
        Assert.Contains("Password=SecretAppPassword123!;", connStr);
        Assert.DoesNotContain("Trusted_Connection=True", connStr);
    }

    [Fact]
    public void DatabaseSecretStore_ProtectAndUnprotect_RoundTripsSecurely()
    {
        var original = "SuperSecretDbPassword#2026";
        var encrypted = DatabaseSecretStore.Protect(original);

        Assert.NotNull(encrypted);
        Assert.NotEqual(original, encrypted);

        var decrypted = DatabaseSecretStore.Unprotect(encrypted);
        Assert.Equal(original, decrypted);
    }

    [Fact]
    public void DatabaseSecretStore_LocalMachineScopeAndFallback_RoundTripsSecurely()
    {
        var original = "LocalMachineSecret#2026";
        var encrypted = DatabaseSecretStore.Protect(original, System.Security.Cryptography.DataProtectionScope.LocalMachine);

        Assert.NotNull(encrypted);
        Assert.NotEqual(original, encrypted);

        // Unprotect should automatically fallback to LocalMachine and decrypt
        var decrypted = DatabaseSecretStore.Unprotect(encrypted);
        Assert.Equal(original, decrypted);
    }

    [Fact]
    public void DatabaseSecretStore_EnumerateDatabases_NullSettings_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => DatabaseSecretStore.EnumerateDatabases(null!));
    }

    [Fact]
    public void DatabaseSecretStore_EnumerateDatabases_UnreachableServer_ReturnsEmptyGracefullyWithoutException()
    {
        var settings = new DatabaseConnectionSettings
        {
            Server = "127.0.0.1,65530",
            ConnectionTimeout = 1
        };

        var databases = DatabaseSecretStore.EnumerateDatabases(settings);

        Assert.NotNull(databases);
        Assert.Empty(databases);
    }

    [Fact]
    public void BuildConnectionString_ManualDatabaseName_PreservesEnteredDatabase()
    {
        var settings = new DatabaseConnectionSettings
        {
            Server = "localhost",
            Database = "Clovent_CustomTenant_Branch01",
            UseWindowsAuthentication = true
        };

        var connStr = settings.BuildConnectionString();

        Assert.Contains("Database=Clovent_CustomTenant_Branch01;", connStr);
    }

    [Fact]
    public void DatabaseSecretStore_Precedence_MachineConfigOverridesUserConfig()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "cbos_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var userPath = Path.Combine(tempDir, "user_db.json");
            var machinePath = Path.Combine(tempDir, "machine_db.json");

            var userSettings = new DatabaseConnectionSettings
            {
                Server = "user-server",
                Database = "UserDb",
                UseWindowsAuthentication = true
            };

            var machineSettings = new DatabaseConnectionSettings
            {
                Server = "machine-server",
                Database = "MachineDb",
                UseWindowsAuthentication = true
            };

            File.WriteAllText(userPath, System.Text.Json.JsonSerializer.Serialize(userSettings));
            File.WriteAllText(machinePath, System.Text.Json.JsonSerializer.Serialize(machineSettings));

            DatabaseSecretStore.SetTestingOverrides(customUserConfigPath: userPath, customMachineConfigPath: machinePath);

            var effectivePath = DatabaseSecretStore.GetConfigFilePath();
            Assert.Equal(machinePath, effectivePath);

            var loaded = DatabaseSecretStore.Load();
            Assert.NotNull(loaded);
            Assert.Equal("machine-server", loaded.Server);
            Assert.Equal("MachineDb", loaded.Database);
        }
        finally
        {
            DatabaseSecretStore.ResetTestingOverrides();
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void DatabaseSecretStore_AtomicSave_WritesValidJsonWithoutTempFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "cbos_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var machinePath = Path.Combine(tempDir, "machine_db.json");
            DatabaseSecretStore.SetTestingOverrides(customMachineConfigPath: machinePath);

            var settings = new DatabaseConnectionSettings
            {
                Server = "prod-sql-cluster",
                Database = "Clovent_Production",
                UseWindowsAuthentication = true
            };

            DatabaseSecretStore.Save(settings, machineLevel: true);

            Assert.True(File.Exists(machinePath));
            var tmpFiles = Directory.GetFiles(tempDir, "*.tmp");
            Assert.Empty(tmpFiles);

            var loaded = DatabaseSecretStore.Load();
            Assert.NotNull(loaded);
            Assert.Equal("prod-sql-cluster", loaded.Server);
            Assert.Equal("Clovent_Production", loaded.Database);
        }
        finally
        {
            DatabaseSecretStore.ResetTestingOverrides();
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
