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
}
