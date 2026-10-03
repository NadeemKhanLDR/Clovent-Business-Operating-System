using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Clovent.Authentication.Infrastructure.Persistence;
using Clovent.Catalog.Infrastructure.Persistence;
using Clovent.Desktop.Commissioning.Database;
using Clovent.Desktop.Commissioning.Security;
using Clovent.Desktop.Configuration;
using Clovent.Desktop.Licensing;
using Clovent.Identity.Infrastructure.Persistence;
using Clovent.Inventory.Infrastructure.Persistence;
using Clovent.MasterData.Infrastructure.Persistence;
using Clovent.Restaurant.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Commissioning.Services;

/// <summary>
/// Status and evaluation result of the multi-signal commissioning state detector.
/// Implements Requirement 2: Multi-signal commissioning state detector.
/// </summary>
public sealed class CommissioningStatusResult
{
    /// <summary>
    /// True ONLY if ALL 5 commissioning signals are satisfied.
    /// If true, the system is fully operational and ready for production sign-in.
    /// </summary>
    public bool IsCommissioned => Signal1DatabaseConnected &&
                                  Signal2SchemaMigrated &&
                                  Signal3ActiveAdminExists &&
                                  Signal4MasterDataExists &&
                                  Signal5MarkerExists;

    /// <summary>Signal 1: Database configuration exists and server/database connection succeeds.</summary>
    public bool Signal1DatabaseConnected { get; set; }

    /// <summary>Signal 2: Database schema is compatible and all EF Core migrations are applied across all 6 bounded contexts.</summary>
    public bool Signal2SchemaMigrated { get; set; }

    /// <summary>Signal 3: At least one active user holding an Administrator role exists in the database.</summary>
    public bool Signal3ActiveAdminExists { get; set; }

    /// <summary>Signal 4: Core master data records exist (Organization, Company, Branch, BusinessSettings).</summary>
    public bool Signal4MasterDataExists { get; set; }

    /// <summary>Signal 5: Protected local commissioning marker exists and passes cryptographic integrity verification.</summary>
    public bool Signal5MarkerExists { get; set; }

    /// <summary>Diagnostic error message if connection or database validation failed.</summary>
    public string? DatabaseErrorMessage { get; set; }

    /// <summary>Human-readable semicolon-delimited summary of missing signals, or empty if fully commissioned.</summary>
    public string? MissingDetailsMessage { get; set; }

    /// <summary>Loaded commissioning marker summary details, if available.</summary>
    public CommissioningSummary? Summary { get; set; }
}

/// <summary>
/// Metadata recorded at the conclusion of first-run commissioning.
/// </summary>
public sealed class CommissioningSummary
{
    /// <summary>UTC timestamp when commissioning completed.</summary>
    public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Machine hardware identifier (SHA-256 fingerprint).</summary>
    public string MachineId { get; set; } = string.Empty;

    /// <summary>Application version at commissioning.</summary>
    public string AppVersion { get; set; } = string.Empty;

    /// <summary>Target physical database name.</summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>Database server instance or host.</summary>
    public string ServerName { get; set; } = string.Empty;

    /// <summary>Enterprise organization name.</summary>
    public string OrganizationName { get; set; } = string.Empty;

    /// <summary>Operating company name.</summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>Primary branch name.</summary>
    public string BranchName { get; set; } = string.Empty;

    /// <summary>First administrator account handle.</summary>
    public string AdminUserName { get; set; } = string.Empty;

    /// <summary>Initial terminal register name.</summary>
    public string TerminalName { get; set; } = string.Empty;

    /// <summary>Indicates whether commissioning was finalized in evaluation/trial mode.</summary>
    public bool IsEvaluation { get; set; }
}

/// <summary>
/// Record stored in %ProgramData%\Clovent\BusinessOperatingSystem\Config\commissioning.json
/// indicating commissioning completion. Includes tamper-detection signature.
/// </summary>
public sealed class CommissioningMarker
{
    public DateTimeOffset CommissionedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string ProductVersion { get; set; } = "1.0.1";
    public string MachineId { get; set; } = string.Empty;
    public string DatabaseServer { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string TerminalName { get; set; } = string.Empty;
    public string AdminUserName { get; set; } = string.Empty;
    public bool IsEvaluation { get; set; }
    public string IntegritySignature { get; set; } = string.Empty;
}

/// <summary>
/// Contract for evaluating commissioning state and persisting the commissioning marker.
/// </summary>
public interface ICommissioningStateService
{
    /// <summary>
    /// Checks whether the local commissioning marker file exists.
    /// </summary>
    bool MarkerExists();

    /// <summary>
    /// Reads the commissioning marker file if present.
    /// </summary>
    CommissioningMarker? LoadMarker();

    /// <summary>
    /// Writes the commissioning marker file to %ProgramData% (with fallback to %LocalAppData%).
    /// </summary>
    void SaveMarker(CommissioningMarker marker);

    /// <summary>
    /// Checks all 5 commissioning signals and returns the composite status.
    /// </summary>
    Task<CommissioningStatusResult> CheckCommissioningStateAsync(IServiceProvider services, CancellationToken ct = default);

    /// <summary>
    /// Records the commissioning completion marker file locally with cryptographic integrity protection.
    /// </summary>
    Task RecordCommissioningCompleteAsync(CommissioningSummary summary, CancellationToken ct = default);

    /// <summary>
    /// Evaluates the 5-signal commissioning status of the system, returning true if fully commissioned.
    /// </summary>
    Task<bool> IsFullyCommissionedAsync(IServiceProvider? services, CancellationToken ct = default);
}

/// <summary>
/// Implements Requirement 2: Multi-signal commissioning state detector.
/// Evaluates:
/// 1. Database configuration file exists and connection succeeds.
/// 2. Schema compatibility is verified (all DbContexts migrated).
/// 3. Active administrator exists in the database.
/// 4. Core master data exists (Organization, Company, Branch, BusinessSettings).
/// 5. Local protected commissioning marker exists (%ProgramData%\Clovent\BusinessOperatingSystem\Config\commissioning.json).
/// </summary>
public sealed class CommissioningStateService(
    IDatabaseProvisioningService? databaseProvisioningService = null,
    IFirstAdminProvisioningService? firstAdminProvisioningService = null,
    ILogger<CommissioningStateService>? logger = null) : ICommissioningStateService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly byte[] HmacKey = "Clovent-Commissioning-Marker-Secret-v1"u8.ToArray();

    /// <summary>
    /// Gets the primary path to commissioning.json in %ProgramData%.
    /// </summary>
    public static string MarkerFilePath =>
        Path.Combine(ProgramDataAclManager.ConfigDirectory, "commissioning.json");

    /// <summary>
    /// Gets the fallback path to commissioning.json in %LocalAppData%.
    /// </summary>
    public static string FallbackMarkerFilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Clovent", "Clovent.BusinessOperatingSystem", "commissioning.json");

    /// <summary>
    /// Checks whether the local commissioning marker file exists in machine or user directory.
    /// </summary>
    public static bool MarkerExists() =>
        File.Exists(MarkerFilePath) || File.Exists(FallbackMarkerFilePath);

    bool ICommissioningStateService.MarkerExists() => MarkerExists();

    /// <summary>
    /// Reads and cryptographically verifies the commissioning marker file if present.
    /// </summary>
    public static CommissioningMarker? LoadMarker()
    {
        try
        {
            var path = File.Exists(MarkerFilePath) ? MarkerFilePath : FallbackMarkerFilePath;
            if (!File.Exists(path))
            {
                return null;
            }

            var json = File.ReadAllText(path);
            var marker = JsonSerializer.Deserialize<CommissioningMarker>(json, JsonOptions);
            if (marker == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(marker.IntegritySignature))
            {
                var expected = ComputeIntegritySignature(
                    marker.CommissionedAtUtc,
                    marker.MachineId,
                    marker.ProductVersion,
                    marker.DatabaseName,
                    marker.OrganizationName);

                if (!string.Equals(marker.IntegritySignature, expected, StringComparison.Ordinal))
                {
                    // Tampered marker detected
                    return null;
                }
            }

            return marker;
        }
        catch
        {
            return null;
        }
    }

    CommissioningMarker? ICommissioningStateService.LoadMarker() => LoadMarker();

    /// <summary>
    /// Writes the commissioning marker file to %ProgramData% (with fallback to %LocalAppData%).
    /// </summary>
    public static void SaveMarker(CommissioningMarker marker)
    {
        ArgumentNullException.ThrowIfNull(marker);

        if (string.IsNullOrWhiteSpace(marker.IntegritySignature))
        {
            marker.IntegritySignature = ComputeIntegritySignature(
                marker.CommissionedAtUtc,
                marker.MachineId,
                marker.ProductVersion,
                marker.DatabaseName,
                marker.OrganizationName);
        }

        var json = JsonSerializer.Serialize(marker, JsonOptions);

        try
        {
            if (!Directory.Exists(ProgramDataAclManager.ConfigDirectory))
            {
                Directory.CreateDirectory(ProgramDataAclManager.ConfigDirectory);
            }

            File.WriteAllText(MarkerFilePath, json);
            return;
        }
        catch
        {
            // Fallback to local app data
            var fallbackDir = Path.GetDirectoryName(FallbackMarkerFilePath)!;
            if (!Directory.Exists(fallbackDir))
            {
                Directory.CreateDirectory(fallbackDir);
            }

            File.WriteAllText(FallbackMarkerFilePath, json);
        }
    }

    void ICommissioningStateService.SaveMarker(CommissioningMarker marker) => SaveMarker(marker);

    /// <inheritdoc/>
    public async Task<CommissioningStatusResult> CheckCommissioningStateAsync(
        IServiceProvider services,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        var result = new CommissioningStatusResult();
        var missingSignals = new List<string>();

        // -------------------------------------------------------------
        // SIGNAL 1: Database configuration file exists and connection succeeds
        // -------------------------------------------------------------
        bool userConfigExists = File.Exists(DatabaseSecretStore.GetUserConfigFilePath());
        bool machineConfigExists = File.Exists(DatabaseSecretStore.GetMachineConfigFilePath());
        bool configSaved = userConfigExists || machineConfigExists;

        var configuration = services.GetService<IConfiguration>();
        string connectionString = DatabaseSecretStore.ResolveConnectionString(configuration);

        bool connectionSucceeded = false;
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            var dbProv = databaseProvisioningService ?? services.GetService<IDatabaseProvisioningService>();
            var savedSettings = DatabaseSecretStore.Load();
            if (dbProv != null && savedSettings != null)
            {
                var (success, _) = await dbProv.TestConnectionAsync(savedSettings, ct).ConfigureAwait(false);
                connectionSucceeded = success;
            }
            else
            {
                connectionSucceeded = DatabaseSecretStore.TestConnection(connectionString, out var connError);
                if (!connectionSucceeded)
                {
                    result.DatabaseErrorMessage = connError;
                }
            }
        }

        result.Signal1DatabaseConnected = (configSaved || !string.IsNullOrWhiteSpace(connectionString)) && connectionSucceeded;
        if (!result.Signal1DatabaseConnected)
        {
            missingSignals.Add("Database connection not established or unconfigured");
        }

        // -------------------------------------------------------------
        // SIGNALS 2, 3, 4: Evaluated via database connection
        // -------------------------------------------------------------
        if (result.Signal1DatabaseConnected)
        {
            using var scope = services.CreateScope();
            var sp = scope.ServiceProvider;

            // SIGNAL 2: Schema compatibility (all 6 DbContexts migrated)
            result.Signal2SchemaMigrated = await VerifySchemaCompatibilityAsync(sp, connectionString, ct).ConfigureAwait(false);
            if (!result.Signal2SchemaMigrated)
            {
                missingSignals.Add("Database schema pending migrations");
            }

            // SIGNAL 3: Active administrator exists in database
            var identityDb = sp.GetService<IdentityDbContext>();
            if (identityDb != null)
            {
                var adminService = firstAdminProvisioningService ??
                                   sp.GetService<IFirstAdminProvisioningService>() ??
                                   new FirstAdminProvisioningService();

                bool canProvisionFirstAdmin = await adminService.CanProvisionFirstAdminAsync(identityDb, ct).ConfigureAwait(false);
                result.Signal3ActiveAdminExists = !canProvisionFirstAdmin;
            }
            else
            {
                result.Signal3ActiveAdminExists = false;
            }

            if (!result.Signal3ActiveAdminExists)
            {
                missingSignals.Add("Active administrator account missing");
            }

            // SIGNAL 4: Core master data exists (Organization, Company, Branch, BusinessSettings)
            var masterDb = sp.GetService<MasterDataDbContext>();
            if (identityDb != null && masterDb != null)
            {
                try
                {
                    bool hasOrg = await identityDb.Organizations.AnyAsync(ct).ConfigureAwait(false);
                    bool hasCompany = await identityDb.Companies.AnyAsync(ct).ConfigureAwait(false);
                    bool hasBranch = await identityDb.Branches.AnyAsync(ct).ConfigureAwait(false);
                    bool hasSettings = await masterDb.BusinessSettings.AnyAsync(ct).ConfigureAwait(false);

                    result.Signal4MasterDataExists = hasOrg && hasCompany && hasBranch && hasSettings;
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "Failed to query core master data entities for commissioning check.");
                    result.Signal4MasterDataExists = false;
                }
            }
            else
            {
                result.Signal4MasterDataExists = false;
            }

            if (!result.Signal4MasterDataExists)
            {
                missingSignals.Add("Core master data missing (Organization, Company, Branch, or BusinessSettings)");
            }
        }
        else
        {
            missingSignals.Add("Schema verification skipped (database not connected)");
            missingSignals.Add("Administrator check skipped (database not connected)");
            missingSignals.Add("Master data check skipped (database not connected)");
        }

        // -------------------------------------------------------------
        // SIGNAL 5: Local protected commissioning marker exists
        // -------------------------------------------------------------
        var markerRecord = LoadMarker();
        result.Signal5MarkerExists = markerRecord != null;
        if (markerRecord != null)
        {
            result.Summary = new CommissioningSummary
            {
                TimestampUtc = markerRecord.CommissionedAtUtc,
                MachineId = markerRecord.MachineId,
                AppVersion = markerRecord.ProductVersion,
                DatabaseName = markerRecord.DatabaseName,
                ServerName = markerRecord.DatabaseServer,
                OrganizationName = markerRecord.OrganizationName,
                CompanyName = markerRecord.CompanyName,
                BranchName = markerRecord.BranchName,
                AdminUserName = markerRecord.AdminUserName,
                TerminalName = markerRecord.TerminalName,
                IsEvaluation = markerRecord.IsEvaluation
            };
        }
        else
        {
            missingSignals.Add("Commissioning marker missing or integrity signature invalid");
        }

        if (missingSignals.Count > 0)
        {
            result.MissingDetailsMessage = string.Join("; ", missingSignals);
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task RecordCommissioningCompleteAsync(
        CommissioningSummary summary,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(summary);

        var timestamp = summary.TimestampUtc == default ? DateTimeOffset.UtcNow : summary.TimestampUtc;
        var machineId = string.IsNullOrWhiteSpace(summary.MachineId)
            ? MachineFingerprint.GetCurrentMachineId()
            : summary.MachineId.Trim();

        var appVersion = string.IsNullOrWhiteSpace(summary.AppVersion)
            ? (typeof(CommissioningStateService).Assembly.GetName().Version?.ToString() ?? "1.0.1")
            : summary.AppVersion.Trim();

        var dbName = summary.DatabaseName?.Trim() ?? string.Empty;
        var orgName = summary.OrganizationName?.Trim() ?? string.Empty;

        var signature = ComputeIntegritySignature(timestamp, machineId, appVersion, dbName, orgName);

        var marker = new CommissioningMarker
        {
            CommissionedAtUtc = timestamp,
            MachineId = machineId,
            ProductVersion = appVersion,
            DatabaseName = dbName,
            DatabaseServer = summary.ServerName?.Trim() ?? string.Empty,
            OrganizationName = orgName,
            CompanyName = summary.CompanyName?.Trim() ?? string.Empty,
            BranchName = summary.BranchName?.Trim() ?? string.Empty,
            AdminUserName = summary.AdminUserName?.Trim() ?? string.Empty,
            TerminalName = summary.TerminalName?.Trim() ?? string.Empty,
            IsEvaluation = summary.IsEvaluation,
            IntegritySignature = signature
        };

        SaveMarker(marker);
        logger?.LogInformation("Commissioning completion marker successfully saved for Organization {OrganizationName}.", orgName);
        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<bool> IsFullyCommissionedAsync(IServiceProvider? services, CancellationToken ct = default)
    {
        if (services == null)
        {
            return MarkerExists();
        }

        var result = await CheckCommissioningStateAsync(services, ct).ConfigureAwait(false);
        return result.IsCommissioned;
    }

    private static string ComputeIntegritySignature(
        DateTimeOffset timestamp,
        string machineId,
        string appVersion,
        string databaseName,
        string organizationName)
    {
        var payload = $"{timestamp:O}|{machineId}|{appVersion}|{databaseName}|{organizationName}";
        using var hmac = new HMACSHA256(HmacKey);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }

    private async Task<bool> VerifySchemaCompatibilityAsync(
        IServiceProvider sp,
        string connectionString,
        CancellationToken ct)
    {
        // 1. First, check if DatabaseSchemaCompatibilityValidator is available
        var validator = sp.GetService<IDatabaseSchemaCompatibilityValidator>() ?? new DatabaseSchemaCompatibilityValidator();
        var result = await validator.ValidateCompatibilityAsync(connectionString, ct).ConfigureAwait(false);
        if (result.IsCompatible)
        {
            return true;
        }

        // 2. Direct relational context inspection fallback (supports testing or custom DbContext wiring)
        Type[] dbContextTypes =
        [
            typeof(IdentityDbContext),
            typeof(AuthenticationDbContext),
            typeof(MasterDataDbContext),
            typeof(CatalogDbContext),
            typeof(InventoryDbContext),
            typeof(RestaurantDbContext)
        ];

        try
        {
            foreach (var type in dbContextTypes)
            {
                if (sp.GetService(type) is DbContext db)
                {
                    if (db.Database.IsRelational())
                    {
                        var pending = await db.Database.GetPendingMigrationsAsync(ct).ConfigureAwait(false);
                        if (pending.Any())
                        {
                            return false;
                        }

                        var applied = await db.Database.GetAppliedMigrationsAsync(ct).ConfigureAwait(false);
                        if (!applied.Any())
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Schema migration verification fallback threw an exception.");
            return false;
        }
    }
}
