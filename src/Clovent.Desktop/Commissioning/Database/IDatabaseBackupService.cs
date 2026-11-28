namespace Clovent.Desktop.Commissioning.Database;

/// <summary>
/// Metadata and verification result for a SQL Server backup file.
/// </summary>
public sealed record BackupVerificationResult(
    bool Success,
    string Message,
    string? DatabaseName = null,
    string? ServerName = null,
    DateTime? BackupFinishDateUtc = null,
    long? BackupSizeBytes = null,
    bool IsCopyOnly = false);

/// <summary>
/// Result of a SQL Server database restore operation.
/// </summary>
public sealed record DatabaseRestoreResult(
    bool Success,
    string TargetDatabase,
    string? SafetyBackupPath,
    string Message);

/// <summary>
/// Service responsible for executing full database backups, integrity verification,
/// and safeguarded database restores.
/// </summary>
public interface IDatabaseBackupService
{
    /// <summary>
    /// Executes a SQL Server database backup with COPY_ONLY and CHECKSUM to the specified or default directory.
    /// Ensures existing backup files are never overwritten and validates that the target path is writable.
    /// </summary>
    Task<(bool Success, string BackupPath, string Message)> CreateBackupAsync(
        string connectionString,
        string? targetDirectory = null,
        CancellationToken ct = default);

    /// <summary>
    /// Verifies the integrity of a SQL Server backup file using RESTORE VERIFYONLY and reads header metadata.
    /// </summary>
    Task<BackupVerificationResult> VerifyBackupAsync(
        string connectionString,
        string backupPath,
        CancellationToken ct = default);

    /// <summary>
    /// Restores a SQL Server database from a verified backup file.
    /// Supports restoring into the configured database or an isolated target database.
    /// </summary>
    Task<DatabaseRestoreResult> RestoreDatabaseAsync(
        string connectionString,
        string backupPath,
        string? targetDatabaseName = null,
        bool createPreRestoreSafetyBackup = true,
        CancellationToken ct = default);
}
