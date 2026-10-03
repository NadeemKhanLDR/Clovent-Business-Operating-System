namespace Clovent.Desktop.Commissioning.Database;

/// <summary>
/// Service responsible for executing full database backups prior to upgrades or schema migrations.
/// Implements Requirement 9: Database backup before upgrade.
/// </summary>
public interface IDatabaseBackupService
{
    /// <summary>
    /// Executes a SQL Server database backup with COPY_ONLY to the specified or default directory.
    /// Ensures existing backup files are never overwritten and validates that the target path is writable.
    /// </summary>
    /// <param name="connectionString">Connection string for the database to back up.</param>
    /// <param name="targetDirectory">Optional target directory. If null or empty, uses SQL Server's default backup directory or local application data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A tuple indicating success status, final backup file path, and a user message.</returns>
    Task<(bool Success, string BackupPath, string Message)> CreateBackupAsync(
        string connectionString,
        string? targetDirectory = null,
        CancellationToken ct = default);
}
