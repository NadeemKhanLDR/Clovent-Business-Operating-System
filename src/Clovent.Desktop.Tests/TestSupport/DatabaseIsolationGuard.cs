using System;
using Microsoft.Data.SqlClient;

namespace Clovent.Desktop.Tests.TestSupport;

/// <summary>
/// Enforces strict database isolation for automated tests.
/// Automated tests must NEVER touch the operational database or run without an explicit isolated disposable test target.
/// </summary>
public static class DatabaseIsolationGuard
{
    public const string IsolatedConnectionEnvVar = "CBOS_TEST_SQL_CONNECTION";
    public const string OperationalDatabaseName = "Clovent_BusinessOperatingSystem";

    /// <summary>
    /// Checks if a valid, non-operational isolated test database connection string is configured in the environment.
    /// </summary>
    public static bool HasIsolatedTestDatabase
    {
        get
        {
            var connStr = Environment.GetEnvironmentVariable(IsolatedConnectionEnvVar);
            if (string.IsNullOrWhiteSpace(connStr))
            {
                return false;
            }

            return !IsOperationalDatabase(connStr);
        }
    }

    /// <summary>
    /// Returns the isolated test database connection string, or throws an InvalidOperationException if none is configured
    /// or if it targets the operational database.
    /// </summary>
    public static string GetIsolatedConnectionStringOrThrow()
    {
        var connStr = Environment.GetEnvironmentVariable(IsolatedConnectionEnvVar);
        if (string.IsNullOrWhiteSpace(connStr))
        {
            throw new InvalidOperationException(
                $"Live SQL Server tests require an explicitly isolated disposable test database specified via the " +
                $"'{IsolatedConnectionEnvVar}' environment variable. Execution without an isolated test target is rejected " +
                $"to prevent workstation or operational database pollution.");
        }

        EnsureIsolated(connStr);
        return connStr;
    }

    /// <summary>
    /// Validates that a given connection string does NOT target the operational database or an unapproved default target.
    /// Throws InvalidOperationException if an isolation violation is detected.
    /// </summary>
    public static void EnsureIsolated(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Database connection string cannot be null or empty.");
        }

        if (IsOperationalDatabase(connectionString))
        {
            throw new InvalidOperationException(
                $"CRITICAL ISOLATION BREACH: Connection string targets the operational database '{OperationalDatabaseName}'. " +
                "Automated tests are strictly forbidden from modifying or connecting to the operational database.");
        }
    }

    /// <summary>
    /// Determines whether the connection string targets the operational database.
    /// </summary>
    public static bool IsOperationalDatabase(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            return string.Equals(builder.InitialCatalog, OperationalDatabaseName, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return connectionString.IndexOf(OperationalDatabaseName, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
