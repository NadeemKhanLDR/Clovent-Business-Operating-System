using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Clovent.Desktop.Configuration;

/// <summary>
/// Handles DPAPI encryption of database credentials and management of local database configuration.
/// Implements Requirement 11 by supporting both CurrentUser and LocalMachine DPAPI scopes with automatic fallback.
/// Ensures plain text passwords are never stored in files or logs.
/// </summary>
public static class DatabaseSecretStore
{
    private static readonly byte[] Entropy = "Clovent-BOS-DbEntropy-v1"u8.ToArray();
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static readonly object SyncLock = new();
    private static string? _customUserConfigPath;
    private static string? _customMachineConfigPath;

    public static void SetTestingOverrides(string? customUserConfigPath = null, string? customMachineConfigPath = null)
    {
        lock (SyncLock)
        {
            _customUserConfigPath = customUserConfigPath;
            _customMachineConfigPath = customMachineConfigPath;
        }
    }

    public static void ResetTestingOverrides()
    {
        lock (SyncLock)
        {
            _customUserConfigPath = null;
            _customMachineConfigPath = null;
        }
    }

    /// <summary>
    /// Gets the path to the machine-level database configuration file in %ProgramData%.
    /// </summary>
    public static string GetMachineConfigFilePath()
    {
        lock (SyncLock)
        {
            if (!string.IsNullOrEmpty(_customMachineConfigPath))
            {
                var customDir = Path.GetDirectoryName(_customMachineConfigPath);
                if (!string.IsNullOrEmpty(customDir) && !Directory.Exists(customDir))
                {
                    Directory.CreateDirectory(customDir);
                }
                return _customMachineConfigPath;
            }
        }

        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var dir = Path.Combine(programData, "Clovent", "BusinessOperatingSystem");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return Path.Combine(dir, "database.config.json");
    }

    /// <summary>
    /// Gets the path to the local user database configuration file in %LocalAppData%.
    /// </summary>
    public static string GetUserConfigFilePath()
    {
        lock (SyncLock)
        {
            if (!string.IsNullOrEmpty(_customUserConfigPath))
            {
                var customDir = Path.GetDirectoryName(_customUserConfigPath);
                if (!string.IsNullOrEmpty(customDir) && !Directory.Exists(customDir))
                {
                    Directory.CreateDirectory(customDir);
                }
                return _customUserConfigPath;
            }
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(localAppData, "Clovent", "Clovent.BusinessOperatingSystem");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        return Path.Combine(dir, "database.config.json");
    }

    /// <summary>
    /// Gets the effective path to the database configuration file.
    /// Prefers the user-level configuration if it exists, otherwise machine-level.
    /// </summary>
    public static string GetConfigFilePath()
    {
        var userPath = GetUserConfigFilePath();
        if (File.Exists(userPath))
        {
            return userPath;
        }

        var machinePath = GetMachineConfigFilePath();
        if (File.Exists(machinePath))
        {
            return machinePath;
        }

        return userPath;
    }

    /// <summary>
    /// Encrypts plaintext using Windows DPAPI with the specified scope (default: CurrentUser).
    /// </summary>
    public static string Protect(string plainText, DataProtectionScope scope = DataProtectionScope.CurrentUser)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return string.Empty;
        }

        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = ProtectedData.Protect(plainBytes, Entropy, scope);
        return Convert.ToBase64String(cipherBytes);
    }

    /// <summary>
    /// Decrypts DPAPI-encrypted ciphertext with automatic fallback between CurrentUser and LocalMachine scopes.
    /// </summary>
    public static string Unprotect(string? cipherTextBase64)
    {
        if (string.IsNullOrWhiteSpace(cipherTextBase64))
        {
            return string.Empty;
        }

        try
        {
            var cipherBytes = Convert.FromBase64String(cipherTextBase64);

            // Attempt CurrentUser scope first
            try
            {
                var plainBytes = ProtectedData.Unprotect(cipherBytes, Entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (CryptographicException)
            {
                // Fall back to LocalMachine scope
                var plainBytes = ProtectedData.Unprotect(cipherBytes, Entropy, DataProtectionScope.LocalMachine);
                return Encoding.UTF8.GetString(plainBytes);
            }
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Loads custom database connection settings from user or machine config if present.
    /// User-level configuration overrides machine-level configuration.
    /// </summary>
    public static DatabaseConnectionSettings? Load()
    {
        var userPath = GetUserConfigFilePath();
        if (File.Exists(userPath))
        {
            try
            {
                var json = File.ReadAllText(userPath);
                var settings = JsonSerializer.Deserialize<DatabaseConnectionSettings>(json);
                if (settings != null)
                {
                    return settings;
                }
            }
            catch
            {
                // Fall through to machine configuration
            }
        }

        var machinePath = GetMachineConfigFilePath();
        if (File.Exists(machinePath))
        {
            try
            {
                var json = File.ReadAllText(machinePath);
                var settings = JsonSerializer.Deserialize<DatabaseConnectionSettings>(json);
                if (settings != null)
                {
                    return settings;
                }
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// Saves custom database connection settings to user-level (or optionally machine-level) config,
    /// securely encrypting passwords with DPAPI and guaranteeing plain text passwords are never stored.
    /// </summary>
    public static void Save(DatabaseConnectionSettings settings, bool machineLevel = false)
    {
        var toSave = settings.Clone();
        var scope = machineLevel ? DataProtectionScope.LocalMachine : DataProtectionScope.CurrentUser;

        if (!string.IsNullOrEmpty(toSave.PlainTextPassword))
        {
            toSave.EncryptedPassword = Protect(toSave.PlainTextPassword, scope);
            toSave.PlainTextPassword = null;
        }

        // Guarantee plain text password is null before persisting
        toSave.PlainTextPassword = null;

        var path = machineLevel ? GetMachineConfigFilePath() : GetUserConfigFilePath();
        var dir = Path.GetDirectoryName(path);
        try
        {
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(toSave, JsonOptions);
            File.WriteAllText(path, json);
        }
        catch (Exception ex) when (machineLevel && (ex is UnauthorizedAccessException || ex is System.Security.SecurityException))
        {
            // Fallback to user-level config if machine-level access is restricted (e.g. non-elevated user or test harness)
            path = GetUserConfigFilePath();
            dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(toSave, JsonOptions);
            File.WriteAllText(path, json);
        }
    }

    /// <summary>
    /// Resolves the effective SQL Server connection string.
    /// Prioritizes saved local connection configuration over appsettings.json.
    /// </summary>
    public static string ResolveConnectionString(IConfiguration? configuration = null)
    {
        var localSettings = Load();
        if (localSettings != null)
        {
            return localSettings.BuildConnectionString();
        }

        var defaultFromConfig = configuration?.GetConnectionString("Default");
        if (!string.IsNullOrWhiteSpace(defaultFromConfig))
        {
            return defaultFromConfig;
        }

        return "Server=.;Database=Clovent_BusinessOperatingSystem;Trusted_Connection=True;TrustServerCertificate=True;";
    }

    /// <summary>
    /// Tests connectivity to the SQL Server database.
    /// </summary>
    public static bool TestConnection(string connectionString, out string? errorMessage)
    {
        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1;";
            command.ExecuteScalar();
            errorMessage = null;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
    }
}
