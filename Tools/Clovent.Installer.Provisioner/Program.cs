using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Desktop.Commissioning.Database;
using Clovent.Desktop.Commissioning.Security;
using Clovent.Desktop.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Clovent.Installer.Provisioner;

/// <summary>
/// Dedicated silent provisioning and migration runner executed by the CBOS installer.
/// Reuses the production DatabaseProvisioningService, EF Core migrations, persistence initializers,
/// PaymentMethodSeeder, ProgramDataAclManager, and DatabaseSecretStore.
/// Guarantees zero plaintext password leakage in logs or console output.
/// </summary>
public static class Program
{
    public const int ExitCodeInvalidArguments = 64;

    public sealed record ProvisionerArguments(
        string Server,
        string Database,
        bool UseWindowsAuth,
        string? UserId,
        string? Password,
        string? LogFilePath,
        bool VerifyOnly,
        bool Provision);

    public static bool TryParseArguments(
        string[]? args,
        out ProvisionerArguments parsed,
        out string? errorMessage)
    {
        parsed = new ProvisionerArguments(
            ".",
            DatabaseConnectionSettings.DefaultDatabaseName,
            true,
            null,
            null,
            null,
            false,
            true);
        errorMessage = null;

        if (args is null)
        {
            return true;
        }

        string server = ".";
        string database = DatabaseConnectionSettings.DefaultDatabaseName;
        bool useWindowsAuth = true;
        bool explicitAuthSpecified = false;
        string? userId = null;
        string? password = null;
        string? logFilePath = null;
        bool verifyOnly = false;
        bool provision = false;

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (string.IsNullOrWhiteSpace(arg))
            {
                errorMessage = $"Empty argument at index {i}.";
                return false;
            }

            if (string.Equals(arg, "--server", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[i + 1]) || args[i + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    errorMessage = "Missing value for --server.";
                    return false;
                }
                server = args[++i];
            }
            else if (string.Equals(arg, "--database", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[i + 1]) || args[i + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    errorMessage = "Missing value for --database.";
                    return false;
                }
                database = args[++i];
            }
            else if (string.Equals(arg, "--auth", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[i + 1]) || args[i + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    errorMessage = "Missing value for --auth (expected 'windows' or 'sql').";
                    return false;
                }
                var val = args[++i];
                if (string.Equals(val, "windows", StringComparison.OrdinalIgnoreCase))
                {
                    useWindowsAuth = true;
                    explicitAuthSpecified = true;
                }
                else if (string.Equals(val, "sql", StringComparison.OrdinalIgnoreCase))
                {
                    useWindowsAuth = false;
                    explicitAuthSpecified = true;
                }
                else
                {
                    errorMessage = $"Invalid --auth mode '{val}'. Expected 'windows' or 'sql'.";
                    return false;
                }
            }
            else if (string.Equals(arg, "--user", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[i + 1]) || args[i + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    errorMessage = "Missing value for --user.";
                    return false;
                }
                userId = args[++i];
                if (!explicitAuthSpecified)
                {
                    useWindowsAuth = false;
                }
            }
            else if (string.Equals(arg, "--password", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[i + 1]) || args[i + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    errorMessage = "Missing value for --password.";
                    return false;
                }
                password = args[++i];
                if (!explicitAuthSpecified)
                {
                    useWindowsAuth = false;
                }
            }
            else if (string.Equals(arg, "--log-file", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[i + 1]) || args[i + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    errorMessage = "Missing value for --log-file.";
                    return false;
                }
                logFilePath = args[++i];
            }
            else if (string.Equals(arg, "--verify-only", StringComparison.OrdinalIgnoreCase))
            {
                verifyOnly = true;
            }
            else if (string.Equals(arg, "--provision", StringComparison.OrdinalIgnoreCase))
            {
                provision = true;
            }
            else
            {
                errorMessage = $"Unrecognized command-line argument '{arg}'.";
                return false;
            }
        }

        if (provision && verifyOnly)
        {
            errorMessage = "Cannot specify both --provision and --verify-only.";
            return false;
        }

        if (!useWindowsAuth && (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(password)))
        {
            errorMessage = "SQL Server authentication (--auth sql) requires both --user and --password.";
            return false;
        }

        if (useWindowsAuth && (!string.IsNullOrWhiteSpace(userId) || !string.IsNullOrWhiteSpace(password)))
        {
            errorMessage = "Windows authentication (--auth windows) cannot be combined with --user or --password.";
            return false;
        }

        if (!provision && !verifyOnly)
        {
            provision = true;
        }

        parsed = new ProvisionerArguments(
            server,
            database,
            useWindowsAuth,
            userId,
            password,
            logFilePath,
            verifyOnly,
            provision);
        return true;
    }

    public static async Task<int> Main(string[] args)
    {
        if (!TryParseArguments(args, out var parsedArgs, out var argError))
        {
            Console.Error.WriteLine($"[ERROR] Invalid provisioner arguments: {argError}");
            return ExitCodeInvalidArguments;
        }

        string server = parsedArgs.Server;
        string database = parsedArgs.Database;
        bool useWindowsAuth = parsedArgs.UseWindowsAuth;
        string? userId = parsedArgs.UserId;
        string? password = parsedArgs.Password;
        string? logFilePath = parsedArgs.LogFilePath;
        bool verifyOnly = parsedArgs.VerifyOnly;
        bool provision = parsedArgs.Provision;

        // Initialize logging
        if (string.IsNullOrWhiteSpace(logFilePath))
        {
            try
            {
                var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                var logDir = Path.Combine(programData, "Clovent", "BusinessOperatingSystem", "Logs");
                Directory.CreateDirectory(logDir);
                logFilePath = Path.Combine(logDir, "installer-provisioning.log");
            }
            catch
            {
                var tempDir = Path.Combine(Path.GetTempPath(), "CloventSetup");
                Directory.CreateDirectory(tempDir);
                logFilePath = Path.Combine(tempDir, "installer-provisioning.log");
            }
        }

        void Log(string message)
        {
            var masked = DatabaseErrorMasker.Mask(message, password);
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff UTC");
            var line = $"[{timestamp}] {masked}";
            Console.WriteLine(masked);
            try
            {
                var dir = Path.GetDirectoryName(logFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.AppendAllText(logFilePath, line + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
                // Ignore log file write errors
            }
        }

        Log("================================================================================");
        Log("CLOVENT BUSINESS OPERATING SYSTEM - INSTALLER DATABASE PROVISIONER");
        Log($"Mode: {(verifyOnly ? "Verify Only" : "Full Provisioning")}");
        Log($"Target Server: {server}");
        Log($"Target Database: {database}");
        Log($"Auth Mode: {(useWindowsAuth ? "Windows Authentication (Integrated)" : $"SQL Server Authentication (User: {userId})")}");
        Log("================================================================================");

        try
        {
            // 1. Configure ProgramData directory tree and ACLs
            Log("Configuring Windows directory security and ACLs for %ProgramData%...");
            if (ProgramDataAclManager.ConfigureDirectorySecurity(out var aclError))
            {
                Log("Protected directory tree and ACLs successfully initialized.");
            }
            else
            {
                Log($"Warning: Could not configure directory ACLs: {aclError}");
            }

            // 2. Build Connection Settings
            var settings = new DatabaseConnectionSettings
            {
                Server = server,
                Database = database,
                UseWindowsAuthentication = useWindowsAuth,
                UserId = useWindowsAuth ? null : userId,
                PlainTextPassword = useWindowsAuth ? null : password,
                TrustServerCertificate = true,
                ConnectionTimeout = 15
            };

            var connectionString = settings.BuildConnectionString();
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddFilter((category, level) => level >= LogLevel.Information);
            });
            var provLogger = loggerFactory.CreateLogger<DatabaseProvisioningService>();
            var provisioningService = new DatabaseProvisioningService(provLogger);

            // 3. Connectivity Verification with Retries
            Log($"Testing connection to SQL Server instance at '{server}' (up to 5 attempts)...");
            bool connected = false;
            string connectMessage = string.Empty;

            for (int attempt = 1; attempt <= 5; attempt++)
            {
                var (success, msg) = await provisioningService.TestConnectionAsync(settings).ConfigureAwait(false);
                if (success)
                {
                    connected = true;
                    connectMessage = msg;
                    Log($"Connection test succeeded on attempt {attempt}: {msg}");
                    break;
                }

                connectMessage = msg;
                Log($"Attempt {attempt}/5: SQL Server not ready ({msg}). Waiting 2s...");
                await Task.Delay(2000).ConfigureAwait(false);
            }

            if (!connected)
            {
                Log($"ERROR: Could not establish connection to SQL Server '{server}': {connectMessage}");
                return 1;
            }

            if (verifyOnly)
            {
                Log("Executing schema compatibility check...");
                var validator = new DatabaseSchemaCompatibilityValidator();
                var result = await validator.ValidateCompatibilityAsync(connectionString).ConfigureAwait(false);
                Log($"Schema Status: {result.Status} - {result.Message}");
                return result.Status == SchemaCompatibilityStatus.Compatible ? 0 : 4;
            }

            // 4. Create Database if missing
            Log($"Ensuring physical database '{database}' exists...");
            var (dbCreated, createDbMsg) = await provisioningService.CreateDatabaseAsync(
                settings,
                adminUserId: useWindowsAuth ? null : userId,
                adminPassword: useWindowsAuth ? null : password).ConfigureAwait(false);

            if (!dbCreated)
            {
                Log($"ERROR: Failed to create database '{database}': {createDbMsg}");
                return 2;
            }
            Log($"Database creation check result: {createDbMsg}");

            // 5. Apply Sequential Bounded-Context EF Migrations and Schema Initializers
            Log("Applying EF Core migrations and persistence initializers across all 6 bounded contexts...");
            var (migrated, migrateMsg) = await provisioningService.ApplyMigrationsAsync(
                connectionString,
                (stage, percent) =>
                {
                    Log($"  [{percent,3}%] {stage}");
                }).ConfigureAwait(false);

            if (!migrated)
            {
                Log($"ERROR: Migration execution failed: {migrateMsg}");
                return 3;
            }
            Log("All bounded-context migrations and schema initializers applied successfully.");

            // 6. Verify Core Payment Methods (Idempotent seed check)
            Log("Verifying core required payment methods in Restaurant bounded context...");
            try
            {
                await using var checkConn = new SqlConnection(connectionString);
                await checkConn.OpenAsync().ConfigureAwait(false);
                await using var checkCmd = checkConn.CreateCommand();
                checkCmd.CommandText = "SELECT [Name], [Status] FROM [Restaurant].[PaymentMethods];";
                await using var reader = await checkCmd.ExecuteReaderAsync().ConfigureAwait(false);

                var methods = new System.Collections.Generic.Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    var mName = reader.GetString(0);
                    var mStatus = reader.GetString(1);
                    methods[mName] = mStatus;
                }

                var required = new[] { "Cash", "Card", "On Account" };
                bool allPresent = true;
                foreach (var req in required)
                {
                    if (methods.TryGetValue(req, out var status) && string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
                    {
                        Log($"  Verified Payment Method: '{req}' (Active)");
                    }
                    else
                    {
                        Log($"  Missing or Inactive Payment Method: '{req}'");
                        allPresent = false;
                    }
                }

                if (!allPresent)
                {
                    Log("ERROR: Core payment methods verification failed.");
                    return 3;
                }
            }
            catch (Exception pmEx)
            {
                Log($"Warning during payment methods verification: {pmEx.Message}");
            }

            // 7. Persist Machine-Level Connection Configuration (DPAPI encrypted)
            Log("Persisting machine-level database configuration in %ProgramData%...");
            DatabaseSecretStore.Save(settings, machineLevel: true);
            var effectiveConfigPath = File.Exists(DatabaseSecretStore.GetMachineConfigFilePath())
                ? DatabaseSecretStore.GetMachineConfigFilePath()
                : DatabaseSecretStore.GetUserConfigFilePath();
            Log($"Database configuration saved successfully to {effectiveConfigPath}.");

            // 8. Validate Schema Compatibility Final Gate
            Log("Executing final database compatibility validation...");
            var compValidator = new DatabaseSchemaCompatibilityValidator();
            var finalResult = await compValidator.ValidateCompatibilityAsync(connectionString).ConfigureAwait(false);
            Log($"Final Schema Compatibility Result: {finalResult.Status} - {finalResult.Message}");

            if (finalResult.Status != SchemaCompatibilityStatus.Compatible)
            {
                Log($"ERROR: Database compatibility check did not return Compatible (Result: {finalResult.Status}).");
                return 4;
            }

            Log("================================================================================");
            Log("DATABASE PROVISIONING AND COMPATIBILITY VALIDATION SUCCEEDED");
            Log("================================================================================");
            return 0;
        }
        catch (Exception ex)
        {
            Log($"FATAL ERROR: Unhandled exception during database provisioning: {ex}");
            return 99;
        }
    }
}
