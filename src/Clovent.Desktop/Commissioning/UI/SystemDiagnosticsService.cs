using System.Reflection;
using System.Text;
using Clovent.Desktop.Commissioning.Database;
using Clovent.Desktop.Commissioning.Security;
using Clovent.Desktop.Commissioning.Services;
using Clovent.Desktop.Configuration;
using Clovent.Desktop.Licensing;
using Microsoft.Data.SqlClient;

namespace Clovent.Desktop.Commissioning.UI;

/// <summary>
/// Collects non-sensitive operational, database, backup, disk-space, and log-health diagnostics
/// suitable for support engineers. Guarantees no passwords or connection strings are exposed.
/// </summary>
public sealed class SystemDiagnosticsService
{
    private const double LowDiskWarningPercent = 10.0;
    private const long LowDiskWarningBytes = 5L * 1024 * 1024 * 1024; // 5 GB
    private const long LargeLogWarningBytes = 500L * 1024 * 1024;   // 500 MB

    private readonly BackupHistoryStore _backupHistoryStore;
    private readonly BackupConfigurationStore _backupConfigStore;
    private readonly string _userLogsDirectory;

    public SystemDiagnosticsService(
        BackupHistoryStore? backupHistoryStore = null,
        BackupConfigurationStore? backupConfigStore = null,
        string? userLogsDirectory = null)
    {
        _backupHistoryStore = backupHistoryStore ?? new BackupHistoryStore();
        _backupConfigStore = backupConfigStore ?? new BackupConfigurationStore();
        _userLogsDirectory = string.IsNullOrWhiteSpace(userLogsDirectory)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Clovent", "Logs")
            : userLogsDirectory;
    }

    public async Task<string> BuildDiagnosticsReportAsync(int deviceDpi = 96, CancellationToken ct = default)
    {
        var sb = new StringBuilder();
        var nowUtc = DateTimeOffset.UtcNow;

        var entryAssembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var appVersion = entryAssembly.GetName().Version?.ToString(3) ?? "1.3.0";
        var infoVersion = entryAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? appVersion;

        var dbSettings = DatabaseSecretStore.Load();
        var serverName = dbSettings?.Server ?? "Not Configured";
        var dbName = dbSettings?.Database ?? DatabaseConnectionSettings.DefaultDatabaseName;
        var isWindowsAuth = dbSettings?.UseWindowsAuthentication ?? true;
        var authMode = isWindowsAuth
            ? "Windows Authentication (Integrated Security)"
            : "SQL Server Authentication (Credentials encrypted via DPAPI)";

        string connectionStatus;
        string schemaStatus = "Not Checked (No Database Configuration)";
        string sqlVersion = "Unknown";
        string outboxSummary = "Unavailable (Database not connected)";

        if (dbSettings != null)
        {
            var testConnStr = dbSettings.BuildConnectionString();
            bool connected = DatabaseSecretStore.TestConnection(testConnStr, out var connError);
            connectionStatus = connected
                ? "Connected (Verified)"
                : $"Connection Failed: {DatabaseErrorMasker.Mask(connError, testConnStr)}";

            if (connected)
            {
                var validator = new DatabaseSchemaCompatibilityValidator();
                var schemaResult = await validator.ValidateCompatibilityAsync(testConnStr, ct).ConfigureAwait(false);
                schemaStatus = $"{schemaResult.Status} - {DatabaseErrorMasker.Mask(schemaResult.Message, testConnStr)}";
                sqlVersion = await QuerySqlVersionAsync(testConnStr, ct).ConfigureAwait(false);
                outboxSummary = await QueryOutboxSummaryAsync(testConnStr, ct).ConfigureAwait(false);
            }
            else
            {
                schemaStatus = "Unavailable (Connection Failed)";
            }
        }
        else
        {
            connectionStatus = "No Saved Configuration";
        }

        var licenseResult = LicenseService.CurrentResult;
        var lic = licenseResult.License;
        var hardwareId = MachineFingerprint.GetCurrentMachineId();

        var marker = CommissioningStateService.LoadMarker();
        var orgName = marker?.OrganizationName ?? "Default Organization";
        var compName = marker?.CompanyName ?? "Default Company";
        var branchName = marker?.BranchName ?? "Main Branch";
        var terminalName = marker?.TerminalName ?? Environment.MachineName;

        var dpiScalePercent = (int)Math.Round(deviceDpi / 96.0 * 100);
        var lastBackup = _backupHistoryStore.GetLastSuccessfulBackup();
        var backupConfig = _backupConfigStore.Load();

        sb.AppendLine("================================================================================");
        sb.AppendLine("CLOVENT BUSINESS OPERATING SYSTEM - SYSTEM SUPPORT DIAGNOSTICS");
        sb.AppendLine($"Timestamp: {nowUtc:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine("================================================================================");
        sb.AppendLine("Application:");
        sb.AppendLine("  Product Name:         Clovent Business Operating System");
        sb.AppendLine($"  Version:              {appVersion} ({infoVersion})");
        sb.AppendLine($"  Target Platform:      win-x64 (.NET {Environment.Version})");
        sb.AppendLine($"  High-DPI Mode:        PerMonitorV2 (Current: {dpiScalePercent}% / {deviceDpi} DPI)");
#if DEBUG
        sb.AppendLine("  Build Configuration:  Debug");
#else
        sb.AppendLine("  Build Configuration:  Release");
#endif

        sb.AppendLine();
        sb.AppendLine("Operating System:");
        sb.AppendLine($"  OS Version:           {Environment.OSVersion.VersionString}");
        sb.AppendLine($"  Host Name:            {Environment.MachineName}");
        sb.AppendLine($"  Current User:         {Environment.UserName}");
        sb.AppendLine($"  Administrator Rights: {(WindowsCommissioningSecurity.IsRunningAsAdministrator() ? "Yes (Elevated)" : "Standard User")}");

        sb.AppendLine();
        sb.AppendLine("Database & Schema:");
        sb.AppendLine($"  Server:               {serverName}");
        sb.AppendLine($"  Database Name:        {dbName}");
        sb.AppendLine($"  Auth Mode:            {authMode}");
        sb.AppendLine($"  Connection Status:    {connectionStatus}");
        sb.AppendLine($"  SQL Server Version:   {sqlVersion}");
        sb.AppendLine($"  Schema Status:        {schemaStatus}");

        sb.AppendLine();
        sb.AppendLine("Backup & Recovery:");
        if (lastBackup != null)
        {
            var sizeMb = lastBackup.SizeBytes.HasValue ? $"{lastBackup.SizeBytes.Value / (1024.0 * 1024.0):F2} MB" : "Unknown";
            sb.AppendLine($"  Last Successful:      {lastBackup.TimestampUtc:yyyy-MM-dd HH:mm:ss} UTC (Verified: {lastBackup.Verified})");
            sb.AppendLine($"  Last Backup File:     {lastBackup.BackupPath} ({sizeMb})");
        }
        else
        {
            sb.AppendLine("  Last Successful:      WARNING - No verified backup recorded");
        }
        sb.AppendLine($"  Backup Directory:     {(string.IsNullOrWhiteSpace(backupConfig.BackupDirectory) ? "SQL Server Default / %ProgramData%\\Clovent\\Backups" : backupConfig.BackupDirectory)}");
        sb.AppendLine($"  Scheduled Backups:    {(backupConfig.ScheduledBackupEnabled ? $"Enabled (Every {backupConfig.ScheduledBackupIntervalHours}h)" : "Disabled")}");

        sb.AppendLine();
        sb.AppendLine("Synchronization & Outbox:");
        sb.AppendLine($"  Outbox Queue:         {outboxSummary}");

        sb.AppendLine();
        sb.AppendLine("Store & Workstation:");
        sb.AppendLine($"  Organization:         {orgName}");
        sb.AppendLine($"  Company:              {compName}");
        sb.AppendLine($"  Branch:               {branchName}");
        sb.AppendLine($"  Terminal:             {terminalName}");

        sb.AppendLine();
        sb.AppendLine("Licensing:");
        sb.AppendLine($"  Status:               {licenseResult.Status}");
        sb.AppendLine($"  License ID:           {(lic != null ? lic.LicenseId.ToString() : "N/A")}");
        sb.AppendLine($"  Licensee:             {(lic != null ? lic.CustomerName : "Unregistered")}");
        sb.AppendLine($"  License Type:         {(lic != null ? lic.LicenseType : "Evaluation / Trial")}");
        sb.AppendLine($"  Expires:              {(lic != null ? lic.ExpiryDate.ToString("yyyy-MM-dd") : "N/A")}");
        sb.AppendLine($"  Hardware ID:          {hardwareId}");
        sb.AppendLine($"  Allowed Terminals:    {(lic != null ? lic.MaxTerminals.ToString() : "N/A")}");

        sb.AppendLine();
        sb.AppendLine("Disk Space Health:");
        AppendDiskHealth(sb, "ProgramData Drive", ProgramDataAclManager.BaseDirectory);
        AppendDiskHealth(sb, "User Logs Drive", _userLogsDirectory);
        if (!string.IsNullOrWhiteSpace(backupConfig.BackupDirectory))
        {
            AppendDiskHealth(sb, "Backup Drive", backupConfig.BackupDirectory);
        }

        sb.AppendLine();
        sb.AppendLine("Log File Health:");
        AppendLogHealth(sb, "Application Logs", _userLogsDirectory);
        AppendLogHealth(sb, "Installer/System Logs", ProgramDataAclManager.LogsDirectory);

        sb.AppendLine();
        sb.AppendLine("Storage & Log Paths:");
        sb.AppendLine($"  ProgramData Directory: {ProgramDataAclManager.BaseDirectory}");
        sb.AppendLine($"  Config Directory:      {ProgramDataAclManager.ConfigDirectory}");
        sb.AppendLine($"  License Directory:     {ProgramDataAclManager.LicenseDirectory}");
        sb.AppendLine($"  System Logs Directory: {ProgramDataAclManager.LogsDirectory}");
        sb.AppendLine($"  App Logs Directory:    {_userLogsDirectory}");
        sb.AppendLine($"  User Config Path:      {DatabaseSecretStore.GetUserConfigFilePath()}");
        sb.AppendLine("================================================================================");

        return sb.ToString();
    }

    internal static void AppendDiskHealth(StringBuilder sb, string label, string path)
    {
        try
        {
            var root = Path.GetPathRoot(Path.GetFullPath(path));
            if (string.IsNullOrWhiteSpace(root))
            {
                sb.AppendLine($"  {label,-21} Unknown drive");
                return;
            }

            var drive = new DriveInfo(root);
            if (!drive.IsReady)
            {
                sb.AppendLine($"  {label,-21} Drive {root} not ready");
                return;
            }

            double freeGb = drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
            double totalGb = drive.TotalSize / (1024.0 * 1024.0 * 1024.0);
            double freePct = drive.TotalSize > 0 ? (drive.AvailableFreeSpace * 100.0 / drive.TotalSize) : 0;

            string status = (drive.AvailableFreeSpace < LowDiskWarningBytes || freePct < LowDiskWarningPercent)
                ? "WARNING - Low Disk Space"
                : "Healthy";

            sb.AppendLine($"  {label,-21} {root} {freeGb:F2} GB free / {totalGb:F2} GB total ({freePct:F1}% free) [{status}]");
        }
        catch (Exception ex)
        {
            sb.AppendLine($"  {label,-21} Unavailable ({ex.Message})");
        }
    }

    internal static void AppendLogHealth(StringBuilder sb, string label, string logDirectory)
    {
        try
        {
            if (!Directory.Exists(logDirectory))
            {
                sb.AppendLine($"  {label,-21} Directory not created yet");
                return;
            }

            var files = new DirectoryInfo(logDirectory).GetFiles("*.log", SearchOption.TopDirectoryOnly);
            long totalBytes = files.Sum(f => f.Length);
            double totalMb = totalBytes / (1024.0 * 1024.0);
            var latest = files.OrderByDescending(f => f.LastWriteTimeUtc).FirstOrDefault();
            string latestText = latest != null ? $"{latest.Name} ({latest.LastWriteTimeUtc:yyyy-MM-dd HH:mm} UTC)" : "None";
            string status = totalBytes > LargeLogWarningBytes ? "WARNING - High Log Volume" : "Healthy";

            sb.AppendLine($"  {label,-21} {files.Length} file(s), {totalMb:F2} MB total, Latest: {latestText} [{status}]");
        }
        catch (Exception ex)
        {
            sb.AppendLine($"  {label,-21} Unavailable ({ex.Message})");
        }
    }

    private static async Task<string> QuerySqlVersionAsync(string connectionString, CancellationToken ct)
    {
        try
        {
            await using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync(ct).ConfigureAwait(false);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT TOP 1 CONCAT(SERVERPROPERTY('ProductVersion'), ' (', SERVERPROPERTY('Edition'), ')');";
            cmd.CommandTimeout = 10;
            var res = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false) as string;
            return string.IsNullOrWhiteSpace(res) ? "Unknown" : res.Trim();
        }
        catch
        {
            return "Unknown";
        }
    }

    private static async Task<string> QueryOutboxSummaryAsync(string connectionString, CancellationToken ct)
    {
        try
        {
            await using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync(ct).ConfigureAwait(false);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                IF OBJECT_ID(N'[Restaurant].[OutboxMessages]', N'U') IS NULL
                    SELECT -1, 0, 0;
                ELSE
                    SELECT
                        SUM(CASE WHEN [Status] = 'Pending' THEN 1 ELSE 0 END),
                        SUM(CASE WHEN [Status] = 'Failed' THEN 1 ELSE 0 END),
                        SUM(CASE WHEN [Status] = 'DeadLetter' THEN 1 ELSE 0 END)
                    FROM [Restaurant].[OutboxMessages];
                """;
            cmd.CommandTimeout = 10;
            await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            if (await reader.ReadAsync(ct).ConfigureAwait(false))
            {
                int pending = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                if (pending < 0) return "Outbox table not initialized";
                int failed = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                int deadLetter = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                return $"Pending: {pending}, Failed: {failed}, DeadLetter: {deadLetter}";
            }
            return "No data";
        }
        catch
        {
            return "Unavailable";
        }
    }
}
