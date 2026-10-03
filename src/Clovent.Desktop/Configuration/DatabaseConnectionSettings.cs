using System.Text.Json.Serialization;

namespace Clovent.Desktop.Configuration;

/// <summary>
/// Model for external database connection configuration supporting Windows Authentication
/// and SQL Server Authentication with DPAPI-encrypted password storage.
/// </summary>
public sealed class DatabaseConnectionSettings
{
    public const string DefaultDatabaseName = "Clovent_BusinessOperatingSystem";
    public const string DefaultServer = ".";

    public string Server { get; set; } = DefaultServer;

    public string Database { get; set; } = DefaultDatabaseName;

    public bool UseWindowsAuthentication { get; set; } = true;

    public string? UserId { get; set; }

    /// <summary>
    /// DPAPI protected password bytes encoded in Base64. Never store plaintext passwords in JSON.
    /// </summary>
    public string? EncryptedPassword { get; set; }

    public bool TrustServerCertificate { get; set; } = true;

    public int ConnectionTimeout { get; set; } = 15;

    [JsonIgnore]
    public string? PlainTextPassword { get; set; }

    /// <summary>
    /// Builds an ADO.NET / EF Core SQL Server connection string from the settings.
    /// </summary>
    public string BuildConnectionString()
    {
        var server = string.IsNullOrWhiteSpace(Server) ? DefaultServer : Server.Trim();
        var database = string.IsNullOrWhiteSpace(Database) ? DefaultDatabaseName : Database.Trim();

        if (UseWindowsAuthentication)
        {
            return $"Server={server};Database={database};Trusted_Connection=True;TrustServerCertificate={TrustServerCertificate};Connection Timeout={ConnectionTimeout};";
        }

        var password = PlainTextPassword;
        if (string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(EncryptedPassword))
        {
            password = DatabaseSecretStore.Unprotect(EncryptedPassword);
        }

        var userId = UserId?.Trim() ?? string.Empty;
        return $"Server={server};Database={database};User Id={userId};Password={password};TrustServerCertificate={TrustServerCertificate};Connection Timeout={ConnectionTimeout};";
    }

    /// <summary>
    /// Clones the current connection settings.
    /// </summary>
    public DatabaseConnectionSettings Clone()
    {
        return new DatabaseConnectionSettings
        {
            Server = Server,
            Database = Database,
            UseWindowsAuthentication = UseWindowsAuthentication,
            UserId = UserId,
            EncryptedPassword = EncryptedPassword,
            PlainTextPassword = PlainTextPassword,
            TrustServerCertificate = TrustServerCertificate,
            ConnectionTimeout = ConnectionTimeout
        };
    }
}
