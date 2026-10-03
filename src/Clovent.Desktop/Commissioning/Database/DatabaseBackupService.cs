using System.Data;
using Clovent.Desktop.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Commissioning.Database;

/// <summary>
/// Implements Requirement 9: Database backup before upgrade.
/// Creates safe, non-destructive COPY_ONLY database backups prior to executing schema migrations or upgrades.
/// </summary>
public sealed class DatabaseBackupService(ILogger<DatabaseBackupService>? logger = null) : IDatabaseBackupService
{
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
        string backupDir = targetDirectory?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(backupDir))
        {
            try
            {
                await using var cmdDefault = connection.CreateCommand();
                cmdDefault.CommandText = "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(512));";
                cmdDefault.CommandTimeout = 15;
                var defaultPath = await cmdDefault.ExecuteScalarAsync(ct).ConfigureAwait(false) as string;
                if (!string.IsNullOrWhiteSpace(defaultPath))
                {
                    backupDir = defaultPath.Trim();
                }
            }
            catch (Exception ex)
            {
                logger?.LogDebug(ex, "Failed to resolve SQL Server default backup path via SERVERPROPERTY.");
            }

            if (string.IsNullOrWhiteSpace(backupDir))
            {
                var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                backupDir = Path.Combine(programData, "Clovent", "Backups");
            }
        }

        // 2. Validate that target path is writable
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

                // If default server directory, the local client process might not have OS access to Program Files,
                // but the SQL Server service account DOES. Log and continue to let SQL Server perform the backup.
                logger?.LogDebug(ex, "Local process cannot write probe to server backup directory '{Dir}'. SQL Server service account will write to it directly.", backupDir);
            }
        }

        // 3. Format backup filename ensuring existing backups are never overwritten
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var baseFileName = $"{dbName}_backup_{timestamp}";
        var backupPath = Path.Combine(backupDir, $"{baseFileName}.bak");

        int counter = 1;
        while (await BackupFileExistsAsync(connection, backupPath, ct).ConfigureAwait(false))
        {
            backupPath = Path.Combine(backupDir, $"{baseFileName}_{counter}.bak");
            counter++;
        }

        // 4. Execute SQL Server backup command
        var escapedDb = dbName.Replace("]", "]]");
        var backupSql = $"BACKUP DATABASE [{escapedDb}] TO DISK = @path WITH COPY_ONLY, FORMAT, INIT;";

        try
        {
            logger?.LogInformation("Starting database backup of '{DatabaseName}' to '{BackupPath}'...", dbName, backupPath);

            await using var cmdBackup = connection.CreateCommand();
            cmdBackup.CommandText = backupSql;
            cmdBackup.Parameters.Add(new SqlParameter("@path", SqlDbType.NVarChar, 4000) { Value = backupPath });
            cmdBackup.CommandTimeout = 300; // 5-minute timeout for database backup operations
            await cmdBackup.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

            logger?.LogInformation("Database backup completed successfully: '{BackupPath}'", backupPath);
            return (true, backupPath, $"Database backup created successfully at '{backupPath}'.");
        }
        catch (SqlException sqlEx)
        {
            var masked = DatabaseErrorMasker.Mask(sqlEx.Message, connectionString);
            logger?.LogError(sqlEx, "SQL Server backup failed (Error {Number}): {Message}", sqlEx.Number, masked);

            string userMessage = sqlEx.Number switch
            {
                3201 => $"Cannot write backup file to '{backupPath}'. SQL Server service account lacks operating system write permissions to the destination folder.",
                _ => $"Database backup failed: {masked}"
            };

            return (false, string.Empty, userMessage);
        }
        catch (Exception ex)
        {
            var masked = DatabaseErrorMasker.Mask(ex.Message, connectionString);
            logger?.LogError(ex, "Database backup failed: {Message}", masked);
            return (false, string.Empty, $"Database backup failed: {masked}");
        }
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
}
