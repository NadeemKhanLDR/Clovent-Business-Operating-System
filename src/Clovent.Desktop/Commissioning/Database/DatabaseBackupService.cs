using System.Data;
using Clovent.Desktop.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Commissioning.Database;

/// <summary>
/// Executes safe, non-destructive COPY_ONLY database backups with CHECKSUM,
/// integrity verification via RESTORE VERIFYONLY, and safeguarded restores.
/// </summary>
public sealed class DatabaseBackupService : IDatabaseBackupService
{
    private readonly ILogger<DatabaseBackupService>? _logger;
    private readonly BackupHistoryStore _historyStore;

    public DatabaseBackupService(
        ILogger<DatabaseBackupService>? logger = null,
        BackupHistoryStore? historyStore = null)
    {
        _logger = logger;
        _historyStore = historyStore ?? new BackupHistoryStore();
    }

    /// <inheritdoc/>
    public async Task<(bool Success, string BackupPath, string Message)> CreateBackupAsync(
        string connectionString,
        string? targetDirectory = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return (false, string.Empty, "Database connection string cannot be empty.");
        }

        SqlConnectionStringBuilder builder;
        try
        {
            builder = new SqlConnectionStringBuilder(connectionString);
        }
        catch (Exception ex)
        {
            return (false, string.Empty, $"Invalid database connection string: {ex.Message}");
        }

        var dbName = string.IsNullOrWhiteSpace(builder.InitialCatalog)
            ? DatabaseConnectionSettings.DefaultDatabaseName
            : builder.InitialCatalog.Trim();

        await using var connection = new SqlConnection(connectionString);
        try
        {
            await connection.OpenAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var masked = DatabaseErrorMasker.Mask(ex.Message, connectionString);
            return (false, string.Empty, $"Cannot connect to database to initiate backup: {masked}");
        }

        // 1. Resolve backup destination directory
        var scope = Clovent.Platform.Configuration.CbosInstanceScope.Current;
        string backupDir = targetDirectory?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(backupDir) && scope.IsQaMode)
        {
            backupDir = scope.BackupsDirectory;
        }

        if (string.IsNullOrWhiteSpace(backupDir))
        {
            backupDir = await ResolveDefaultBackupDirectoryAsync(connection, ct).ConfigureAwait(false);
        }

        bool directoryAccessibleLocally = false;
        try
        {
            if (!Directory.Exists(backupDir))
            {
                Directory.CreateDirectory(backupDir);
            }
            directoryAccessibleLocally = true;
        }
        catch (Exception ex) when (!string.IsNullOrWhiteSpace(targetDirectory))
        {
            return (false, string.Empty, $"Target directory '{backupDir}' cannot be created or accessed: {ex.Message}");
        }
        catch
        {
            // Directory may be hosted on a remote SQL Server machine not locally accessible
        }

        if (directoryAccessibleLocally)
        {
            try
            {
                var probeFile = Path.Combine(backupDir, $".write_test_{Guid.NewGuid():N}.tmp");
                using (var fs = new FileStream(probeFile, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.DeleteOnClose))
                {
                    fs.WriteByte(1);
                }
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrWhiteSpace(targetDirectory))
                {
                    return (false, string.Empty, $"Target directory '{backupDir}' is not writable: {ex.Message}");
                }

                _logger?.LogDebug(ex, "Local process cannot write probe to server backup directory '{Dir}'. SQL Server service account will write to it directly.", backupDir);
            }
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var baseFileName = $"{dbName}_backup_{timestamp}";
        var backupPath = Path.Combine(backupDir, $"{baseFileName}.bak");

        int counter = 1;
        while (await BackupFileExistsAsync(connection, backupPath, ct).ConfigureAwait(false))
        {
            backupPath = Path.Combine(backupDir, $"{baseFileName}_{counter}.bak");
            counter++;
        }

        var escapedDb = dbName.Replace("]", "]]");
        var backupSql = $"BACKUP DATABASE [{escapedDb}] TO DISK = @path WITH COPY_ONLY, FORMAT, INIT, CHECKSUM;";

        try
        {
            _logger?.LogInformation("Starting database backup of '{DatabaseName}' to '{BackupPath}'...", dbName, backupPath);

            await using var cmdBackup = connection.CreateCommand();
            cmdBackup.CommandText = backupSql;
            cmdBackup.Parameters.Add(new SqlParameter("@path", SqlDbType.NVarChar, 4000) { Value = backupPath });
            cmdBackup.CommandTimeout = 300;
            await cmdBackup.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

            var verifyResult = await VerifyBackupInternalAsync(connection, backupPath, ct).ConfigureAwait(false);
            long? fileSize = File.Exists(backupPath) ? new FileInfo(backupPath).Length : verifyResult.BackupSizeBytes;

            if (!verifyResult.Success)
            {
                var failMsg = $"Backup file was written to '{backupPath}', but integrity verification failed: {verifyResult.Message}";
                TryRecordHistory("Backup", dbName, backupPath, fileSize, verified: false, success: false, failMsg);
                return (false, backupPath, failMsg);
            }

            var okMsg = $"Database backup created and verified successfully at '{backupPath}'.";
            TryRecordHistory("Backup", dbName, backupPath, fileSize, verified: true, success: true, okMsg);
            _logger?.LogInformation("Database backup completed and verified successfully: '{BackupPath}'", backupPath);
            return (true, backupPath, okMsg);
        }
        catch (SqlException sqlEx)
        {
            var masked = DatabaseErrorMasker.Mask(sqlEx.Message, connectionString);
            _logger?.LogError(sqlEx, "SQL Server backup failed (Error {Number}): {Message}", sqlEx.Number, masked);

            string userMessage = sqlEx.Number switch
            {
                3201 => $"Cannot write backup file to '{backupPath}'. SQL Server service account lacks operating system write permissions to the destination folder.",
                _ => $"Database backup failed: {masked}"
            };

            TryRecordHistory("Backup", dbName, backupPath, null, verified: false, success: false, userMessage);
            return (false, string.Empty, userMessage);
        }
        catch (Exception ex)
        {
            var masked = DatabaseErrorMasker.Mask(ex.Message, connectionString);
            _logger?.LogError(ex, "Database backup failed: {Message}", masked);
            var msg = $"Database backup failed: {masked}";
            TryRecordHistory("Backup", dbName, backupPath, null, verified: false, success: false, msg);
            return (false, string.Empty, msg);
        }
    }

    /// <inheritdoc/>
    public async Task<BackupVerificationResult> VerifyBackupAsync(
        string connectionString,
        string backupPath,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new BackupVerificationResult(false, "Database connection string cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(backupPath))
        {
            return new BackupVerificationResult(false, "Backup file path cannot be empty.");
        }

        SqlConnectionStringBuilder builder;
        try
        {
            builder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };
        }
        catch (Exception ex)
        {
            return new BackupVerificationResult(false, $"Invalid database connection string: {ex.Message}");
        }

        await using var connection = new SqlConnection(builder.ConnectionString);
        try
        {
            await connection.OpenAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var masked = DatabaseErrorMasker.Mask(ex.Message, connectionString);
            return new BackupVerificationResult(false, $"Cannot connect to SQL Server to verify backup: {masked}");
        }

        if (!await BackupFileExistsAsync(connection, backupPath, ct).ConfigureAwait(false))
        {
            return new BackupVerificationResult(false, $"Backup file '{backupPath}' was not found or is inaccessible to SQL Server.");
        }

        var result = await VerifyBackupInternalAsync(connection, backupPath, ct).ConfigureAwait(false);
        TryRecordHistory(
            "Verify",
            result.DatabaseName ?? DatabaseConnectionSettings.DefaultDatabaseName,
            backupPath,
            result.BackupSizeBytes,
            verified: result.Success,
            success: result.Success,
            result.Message);
        return result;
    }

    /// <inheritdoc/>
    public async Task<DatabaseRestoreResult> RestoreDatabaseAsync(
        string connectionString,
        string backupPath,
        string? targetDatabaseName = null,
        bool createPreRestoreSafetyBackup = true,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new DatabaseRestoreResult(false, string.Empty, null, "Database connection string cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(backupPath))
        {
            return new DatabaseRestoreResult(false, string.Empty, null, "Backup file path cannot be empty.");
        }

        SqlConnectionStringBuilder builder;
        try
        {
            builder = new SqlConnectionStringBuilder(connectionString);
        }
        catch (Exception ex)
        {
            return new DatabaseRestoreResult(false, string.Empty, null, $"Invalid database connection string: {ex.Message}");
        }

        var targetDb = string.IsNullOrWhiteSpace(targetDatabaseName)
            ? (string.IsNullOrWhiteSpace(builder.InitialCatalog) ? DatabaseConnectionSettings.DefaultDatabaseName : builder.InitialCatalog.Trim())
            : targetDatabaseName.Trim();

        // 1. Verify backup integrity before modifying any database
        var verification = await VerifyBackupAsync(connectionString, backupPath, ct).ConfigureAwait(false);
        if (!verification.Success)
        {
            return new DatabaseRestoreResult(false, targetDb, null, $"Restore aborted because backup verification failed: {verification.Message}");
        }

        var masterBuilder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };
        await using var masterConn = new SqlConnection(masterBuilder.ConnectionString);
        try
        {
            await masterConn.OpenAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            var masked = DatabaseErrorMasker.Mask(ex.Message, connectionString);
            return new DatabaseRestoreResult(false, targetDb, null, $"Cannot connect to SQL Server master database: {masked}");
        }

        bool targetExists = await DatabaseExistsAsync(masterConn, targetDb, ct).ConfigureAwait(false);
        string? safetyBackupPath = null;

        // 2. Create a pre-restore safety backup if overwriting an existing database
        if (targetExists && createPreRestoreSafetyBackup)
        {
            var targetConnBuilder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = targetDb };
            var backupDir = Path.GetDirectoryName(backupPath);
            var (safetyOk, safetyPath, safetyMsg) = await CreateBackupAsync(
                targetConnBuilder.ConnectionString,
                string.IsNullOrWhiteSpace(backupDir) ? null : backupDir,
                ct).ConfigureAwait(false);

            if (!safetyOk)
            {
                var abortMsg = $"Restore aborted: unable to create pre-restore safety backup of '{targetDb}': {safetyMsg}";
                TryRecordHistory("Restore", targetDb, backupPath, verification.BackupSizeBytes, verified: true, success: false, abortMsg);
                return new DatabaseRestoreResult(false, targetDb, null, abortMsg);
            }

            safetyBackupPath = safetyPath;
        }

        // 3. Build MOVE clauses if restoring to a new or differently named database
        var moveClauses = new List<string>();
        bool needsMove = !targetExists ||
                         string.IsNullOrWhiteSpace(verification.DatabaseName) ||
                         !string.Equals(verification.DatabaseName, targetDb, StringComparison.OrdinalIgnoreCase);

        if (needsMove)
        {
            var logicalFiles = await ReadFileListAsync(masterConn, backupPath, ct).ConfigureAwait(false);
            if (logicalFiles.Count > 0)
            {
                var dataDir = await QueryServerPropertyPathAsync(masterConn, "InstanceDefaultDataPath", ct).ConfigureAwait(false)
                              ?? Path.GetDirectoryName(logicalFiles[0].PhysicalName)
                              ?? Path.GetTempPath();
                var logDir = await QueryServerPropertyPathAsync(masterConn, "InstanceDefaultLogPath", ct).ConfigureAwait(false)
                             ?? dataDir;

                var safeFilePrefix = MakeSafeFileName(targetDb);
                var uniqueSuffix = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                int fileIndex = 0;

                foreach (var (logicalName, _, fileType) in logicalFiles)
                {
                    bool isLog = string.Equals(fileType, "L", StringComparison.OrdinalIgnoreCase);
                    var ext = isLog ? ".ldf" : (fileIndex == 0 ? ".mdf" : $".ndf");
                    var dir = isLog ? logDir : dataDir;
                    var destPath = Path.Combine(dir, $"{safeFilePrefix}_{uniqueSuffix}_{fileIndex}{ext}");
                    var escapedLogical = logicalName.Replace("'", "''");
                    var escapedDest = destPath.Replace("'", "''");
                    moveClauses.Add($"MOVE N'{escapedLogical}' TO N'{escapedDest}'");
                    fileIndex++;
                }
            }
        }

        var escapedTargetDb = targetDb.Replace("]", "]]");
        bool setSingleUser = false;

        try
        {
            // Clear pooled connections that might hold locks on targetDb
            SqlConnection.ClearAllPools();

            if (targetExists)
            {
                await using var singleUserCmd = masterConn.CreateCommand();
                singleUserCmd.CommandText = $"ALTER DATABASE [{escapedTargetDb}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;";
                singleUserCmd.CommandTimeout = 60;
                await singleUserCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                setSingleUser = true;
            }

            var moveSql = moveClauses.Count > 0 ? ", " + string.Join(", ", moveClauses) : string.Empty;
            await using var restoreCmd = masterConn.CreateCommand();
            restoreCmd.CommandText = $"RESTORE DATABASE [{escapedTargetDb}] FROM DISK = @path WITH REPLACE, RECOVERY{moveSql};";
            restoreCmd.Parameters.Add(new SqlParameter("@path", SqlDbType.NVarChar, 4000) { Value = backupPath });
            restoreCmd.CommandTimeout = 600;
            await restoreCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

            var okMsg = safetyBackupPath is not null
                ? $"Database '{targetDb}' restored successfully from '{backupPath}'. Safety backup saved at '{safetyBackupPath}'."
                : $"Database '{targetDb}' restored successfully from '{backupPath}'.";

            TryRecordHistory("Restore", targetDb, backupPath, verification.BackupSizeBytes, verified: true, success: true, okMsg);
            _logger?.LogInformation("Database '{DatabaseName}' restored successfully from '{BackupPath}'.", targetDb, backupPath);
            return new DatabaseRestoreResult(true, targetDb, safetyBackupPath, okMsg);
        }
        catch (Exception ex)
        {
            var masked = DatabaseErrorMasker.Mask(ex.Message, connectionString);
            var failMsg = $"Database restore failed: {masked}";
            _logger?.LogError(ex, "Database restore failed for '{DatabaseName}': {Message}", targetDb, masked);
            TryRecordHistory("Restore", targetDb, backupPath, verification.BackupSizeBytes, verified: true, success: false, failMsg);
            return new DatabaseRestoreResult(false, targetDb, safetyBackupPath, failMsg);
        }
        finally
        {
            if (setSingleUser)
            {
                try
                {
                    await using var multiUserCmd = masterConn.CreateCommand();
                    multiUserCmd.CommandText = $"IF DB_ID(@dbName) IS NOT NULL ALTER DATABASE [{escapedTargetDb}] SET MULTI_USER;";
                    multiUserCmd.Parameters.Add(new SqlParameter("@dbName", SqlDbType.NVarChar, 256) { Value = targetDb });
                    multiUserCmd.CommandTimeout = 30;
                    await multiUserCmd.ExecuteNonQueryAsync(CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Failed to reset database '{DatabaseName}' to MULTI_USER after restore.", targetDb);
                }
            }
        }
    }

    private async Task<BackupVerificationResult> VerifyBackupInternalAsync(
        SqlConnection connection,
        string backupPath,
        CancellationToken ct)
    {
        try
        {
            await using var verifyCmd = connection.CreateCommand();
            verifyCmd.CommandText = "RESTORE VERIFYONLY FROM DISK = @path;";
            verifyCmd.Parameters.Add(new SqlParameter("@path", SqlDbType.NVarChar, 4000) { Value = backupPath });
            verifyCmd.CommandTimeout = 300;
            await verifyCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

            string? dbName = null;
            string? serverName = null;
            DateTime? finishDate = null;
            long? backupSize = null;
            bool isCopyOnly = false;

            try
            {
                await using var headerCmd = connection.CreateCommand();
                headerCmd.CommandText = "RESTORE HEADERONLY FROM DISK = @path;";
                headerCmd.Parameters.Add(new SqlParameter("@path", SqlDbType.NVarChar, 4000) { Value = backupPath });
                headerCmd.CommandTimeout = 60;
                await using var reader = await headerCmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                if (await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    dbName = GetNullableString(reader, "DatabaseName");
                    serverName = GetNullableString(reader, "ServerName");
                    finishDate = GetNullableDateTime(reader, "BackupFinishDate");
                    backupSize = GetNullableInt64(reader, "BackupSize");
                    isCopyOnly = GetNullableBool(reader, "IsCopyOnly");
                }
            }
            catch (Exception hdrEx)
            {
                _logger?.LogDebug(hdrEx, "Could not read RESTORE HEADERONLY metadata for '{BackupPath}'.", backupPath);
            }

            return new BackupVerificationResult(
                true,
                $"Backup '{backupPath}' passed RESTORE VERIFYONLY integrity verification.",
                dbName,
                serverName,
                finishDate,
                backupSize,
                isCopyOnly);
        }
        catch (Exception ex)
        {
            var masked = DatabaseErrorMasker.Mask(ex.Message);
            return new BackupVerificationResult(false, $"Backup verification failed: {masked}");
        }
    }

    private async Task<string> ResolveDefaultBackupDirectoryAsync(SqlConnection connection, CancellationToken ct)
    {
        var configured = new BackupConfigurationStore().Load().BackupDirectory;
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.Trim();
        }

        var defaultPath = await QueryServerPropertyPathAsync(connection, "InstanceDefaultBackupPath", ct).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(defaultPath))
        {
            return defaultPath;
        }

        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        return Path.Combine(programData, "Clovent", "Backups");
    }

    private async Task<string?> QueryServerPropertyPathAsync(SqlConnection connection, string propertyName, CancellationToken ct)
    {
        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT CAST(SERVERPROPERTY('{propertyName}') AS NVARCHAR(512));";
            cmd.CommandTimeout = 15;
            var result = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false) as string;
            return string.IsNullOrWhiteSpace(result) ? null : result.Trim();
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Failed to resolve SQL Server property '{Property}'.", propertyName);
            return null;
        }
    }

    private static async Task<bool> DatabaseExistsAsync(SqlConnection masterConn, string databaseName, CancellationToken ct)
    {
        await using var cmd = masterConn.CreateCommand();
        cmd.CommandText = "SELECT CASE WHEN DB_ID(@dbName) IS NOT NULL THEN 1 ELSE 0 END;";
        cmd.Parameters.Add(new SqlParameter("@dbName", SqlDbType.NVarChar, 256) { Value = databaseName });
        cmd.CommandTimeout = 15;
        var res = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
        return Convert.ToInt32(res) == 1;
    }

    private static async Task<List<(string LogicalName, string PhysicalName, string FileType)>> ReadFileListAsync(
        SqlConnection masterConn,
        string backupPath,
        CancellationToken ct)
    {
        var files = new List<(string LogicalName, string PhysicalName, string FileType)>();
        await using var cmd = masterConn.CreateCommand();
        cmd.CommandText = "RESTORE FILELISTONLY FROM DISK = @path;";
        cmd.Parameters.Add(new SqlParameter("@path", SqlDbType.NVarChar, 4000) { Value = backupPath });
        cmd.CommandTimeout = 60;
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            var logical = GetNullableString(reader, "LogicalName") ?? string.Empty;
            var physical = GetNullableString(reader, "PhysicalName") ?? string.Empty;
            var type = GetNullableString(reader, "Type") ?? "D";
            if (!string.IsNullOrWhiteSpace(logical))
            {
                files.Add((logical, physical, type));
            }
        }
        return files;
    }

    private static async Task<bool> BackupFileExistsAsync(SqlConnection connection, string path, CancellationToken ct)
    {
        if (File.Exists(path))
        {
            return true;
        }

        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "DECLARE @exists INT; EXEC master.dbo.xp_fileexist @path, @exists OUTPUT; SELECT @exists;";
            cmd.Parameters.Add(new SqlParameter("@path", SqlDbType.NVarChar, 4000) { Value = path });
            cmd.CommandTimeout = 10;
            var res = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
            return Convert.ToInt32(res) == 1;
        }
        catch
        {
            return false;
        }
    }

    private void TryRecordHistory(
        string operation,
        string databaseName,
        string backupPath,
        long? sizeBytes,
        bool verified,
        bool success,
        string message)
    {
        try
        {
            _historyStore.Record(new BackupHistoryEntry(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                operation,
                databaseName,
                backupPath,
                sizeBytes,
                verified,
                success,
                message,
                Environment.UserName));
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Failed to record backup history entry.");
        }
    }

    private static string MakeSafeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
    }

    private static string? GetNullableString(SqlDataReader reader, string columnName)
    {
        var ord = TryGetOrdinal(reader, columnName);
        return ord >= 0 && !reader.IsDBNull(ord) ? Convert.ToString(reader.GetValue(ord)) : null;
    }

    private static DateTime? GetNullableDateTime(SqlDataReader reader, string columnName)
    {
        var ord = TryGetOrdinal(reader, columnName);
        return ord >= 0 && !reader.IsDBNull(ord) ? Convert.ToDateTime(reader.GetValue(ord)) : null;
    }

    private static long? GetNullableInt64(SqlDataReader reader, string columnName)
    {
        var ord = TryGetOrdinal(reader, columnName);
        return ord >= 0 && !reader.IsDBNull(ord) ? Convert.ToInt64(reader.GetValue(ord)) : null;
    }

    private static bool GetNullableBool(SqlDataReader reader, string columnName)
    {
        var ord = TryGetOrdinal(reader, columnName);
        return ord >= 0 && !reader.IsDBNull(ord) && Convert.ToBoolean(reader.GetValue(ord));
    }

    private static int TryGetOrdinal(SqlDataReader reader, string columnName)
    {
        for (int i = 0; i < reader.FieldCount; i++)
        {
            if (string.Equals(reader.GetName(i), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }
        return -1;
    }
}
