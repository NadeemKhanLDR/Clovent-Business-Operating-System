using Clovent.Desktop.Configuration;

namespace Clovent.Desktop.Commissioning.Database;

/// <summary>
/// Service responsible for testing SQL connectivity, checking database existence,
/// creating the central database, and applying all EF Core migrations and schema initializations.
/// </summary>
public interface IDatabaseProvisioningService
{
    /// <summary>
    /// Tests connectivity to the SQL Server database with a 10-second timeout,
    /// ensuring any raw passwords or sensitive credentials in error messages are masked.
    /// </summary>
    Task<(bool Success, string Message)> TestConnectionAsync(DatabaseConnectionSettings settings, CancellationToken ct = default);

    /// <summary>
    /// Checks whether the target database exists on the configured SQL Server instance.
    /// </summary>
    Task<bool> DatabaseExistsAsync(DatabaseConnectionSettings settings, CancellationToken ct = default);

    /// <summary>
    /// Creates the database (defaulting to [Clovent_BusinessOperatingSystem]) using administrative or Windows Authentication credentials.
    /// If creation fails due to insufficient permissions, provides clear DBA guidance.
    /// </summary>
    Task<(bool Success, string Message)> CreateDatabaseAsync(DatabaseConnectionSettings settings, string? adminUserId = null, string? adminPassword = null, CancellationToken ct = default);

    /// <summary>
    /// Sequentially applies EF Core migrations for all bounded contexts and runs the Restaurant schema DDL and seeds.
    /// </summary>
    Task<(bool Success, string Message)> ApplyMigrationsAsync(string connectionString, Action<string, int>? progressCallback = null, CancellationToken ct = default);
}
