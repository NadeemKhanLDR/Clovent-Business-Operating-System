using Clovent.Authentication.Infrastructure.Persistence;
using Clovent.Catalog.Infrastructure.Persistence;
using Clovent.Identity.Infrastructure.Persistence;
using Clovent.Inventory.Infrastructure.Persistence;
using Clovent.MasterData.Infrastructure.Persistence;
using Clovent.Restaurant.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Commissioning.Database;

/// <summary>
/// Implements Requirement 8: Automatic EF migration safety.
/// During normal POS and application startup, verifies schema compatibility across all bounded contexts
/// without blindly applying destructive migrations or modifying database state.
/// </summary>
public sealed class DatabaseSchemaCompatibilityValidator(ILogger<DatabaseSchemaCompatibilityValidator>? logger = null)
    : IDatabaseSchemaCompatibilityValidator
{
    /// <inheritdoc/>
    public async Task<SchemaCompatibilityResult> ValidateCompatibilityAsync(
        string connectionString,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return new SchemaCompatibilityResult(
                SchemaCompatibilityStatus.ConnectionFailed,
                "No database connection string has been configured. Please configure your database settings.",
                "Connection string was null or empty.");
        }

        // 1. Connectivity and database existence pre-check
        try
        {
            var testBuilder = new SqlConnectionStringBuilder(connectionString)
            {
                ConnectTimeout = 10
            };

            await using var conn = new SqlConnection(testBuilder.ConnectionString);
            await conn.OpenAsync(ct).ConfigureAwait(false);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1;";
            cmd.CommandTimeout = 10;
            await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
        }
        catch (SqlException sqlEx)
        {
            logger?.LogWarning(sqlEx, "Database connection validation failed (Error {Number})", sqlEx.Number);
            var masked = DatabaseErrorMasker.Mask(sqlEx.Message, connectionString);

            string userFriendly = sqlEx.Number switch
            {
                4060 => "The target database does not exist on the server. Please run initial database provisioning.",
                18456 => "Database authentication failed. Please check the configured username and password.",
                53 or 258 => "Cannot establish a connection to the SQL Server. Please verify the server address and network connectivity.",
                _ => "Unable to connect to the database. Please verify your connection settings."
            };

            return new SchemaCompatibilityResult(
                SchemaCompatibilityStatus.ConnectionFailed,
                userFriendly,
                masked);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Unexpected error establishing connection to database");
            var masked = DatabaseErrorMasker.Mask(ex.Message, connectionString);
            return new SchemaCompatibilityResult(
                SchemaCompatibilityStatus.ConnectionFailed,
                "Unable to connect to the database.",
                masked);
        }

        // 2. Inspect migrations across all six bounded contexts
        var contexts = new (string Name, Func<DbContext> Factory)[]
        {
            ("Authentication", () => new AuthenticationDbContext(
                new DbContextOptionsBuilder<AuthenticationDbContext>()
                    .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "Authentication"))
                    .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
                    .Options)),
            ("Identity", () => new IdentityDbContext(
                new DbContextOptionsBuilder<IdentityDbContext>()
                    .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "Identity"))
                    .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
                    .Options)),
            ("MasterData", () => new MasterDataDbContext(
                new DbContextOptionsBuilder<MasterDataDbContext>()
                    .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "MasterData"))
                    .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
                    .Options)),
            ("Catalog", () => new CatalogDbContext(
                new DbContextOptionsBuilder<CatalogDbContext>()
                    .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "Catalog"))
                    .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
                    .Options)),
            ("Inventory", () => new InventoryDbContext(
                new DbContextOptionsBuilder<InventoryDbContext>()
                    .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "Inventory"))
                    .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
                    .Options)),
            ("Restaurant", () => new RestaurantDbContext(
                new DbContextOptionsBuilder<RestaurantDbContext>()
                    .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "Restaurant"))
                    .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
                    .Options)),
        };

        var allPending = new List<string>();
        var allUnknown = new List<string>();
        var pendingByContext = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        var unknownByContext = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var (name, factory) in contexts)
        {
            try
            {
                await using var dbContext = factory();

                var defined = dbContext.Database.GetMigrations().ToHashSet(StringComparer.OrdinalIgnoreCase);
                var appliedList = await dbContext.Database.GetAppliedMigrationsAsync(ct).ConfigureAwait(false);
                var pendingList = await dbContext.Database.GetPendingMigrationsAsync(ct).ConfigureAwait(false);

                var appliedSet = appliedList.ToHashSet(StringComparer.OrdinalIgnoreCase);
                var unknown = appliedSet.Where(m => !defined.Contains(m)).ToList();
                var pending = pendingList.ToList();

                if (pending.Count > 0)
                {
                    allPending.AddRange(pending);
                    pendingByContext[name] = pending;
                }

                if (unknown.Count > 0)
                {
                    allUnknown.AddRange(unknown);
                    unknownByContext[name] = unknown;
                }
            }
            catch (Exception ex)
            {
                var masked = DatabaseErrorMasker.Mask(ex.Message, connectionString);
                logger?.LogError(ex, "Failed querying migration history for {Context}: {Error}", name, masked);
                return new SchemaCompatibilityResult(
                    SchemaCompatibilityStatus.ConnectionFailed,
                    $"Failed to inspect database schema compatibility for {name}.",
                    masked);
            }
        }

        // 3. Determine compatibility status
        if (allUnknown.Count > 0)
        {
            logger?.LogWarning("Database contains {Count} unknown migrations from a newer version", allUnknown.Count);
            return new SchemaCompatibilityResult(
                SchemaCompatibilityStatus.DatabaseNewer,
                "The database was updated by a newer version of the application and is incompatible with this release. Please update your application to the latest version.",
                $"Found unknown migrations: {string.Join(", ", allUnknown)}",
                allPending,
                allUnknown,
                pendingByContext,
                unknownByContext);
        }

        if (allPending.Count > 0)
        {
            logger?.LogInformation("Database has {Count} pending migrations that must be applied", allPending.Count);
            return new SchemaCompatibilityResult(
                SchemaCompatibilityStatus.DatabaseTooOld,
                "The database schema is outdated. Required updates must be applied before normal application startup.",
                $"Pending migrations: {string.Join(", ", allPending)}",
                allPending,
                allUnknown,
                pendingByContext,
                unknownByContext);
        }

        // 4. Verify live physical SQL Server catalog against compiled EF Core models
        try
        {
            var existingTables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var existingIndexes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var catalogBuilder = new SqlConnectionStringBuilder(connectionString)
            {
                ConnectTimeout = 10
            };

            await using (var conn = new SqlConnection(catalogBuilder.ConnectionString))
            {
                await conn.OpenAsync(ct).ConfigureAwait(false);

                await using (var tableCmd = conn.CreateCommand())
                {
                    tableCmd.CommandText = """
                        SELECT s.name AS SchemaName, t.name AS TableName
                        FROM sys.tables t
                        INNER JOIN sys.schemas s ON t.schema_id = s.schema_id;
                        """;
                    tableCmd.CommandTimeout = 15;
                    await using var reader = await tableCmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                    while (await reader.ReadAsync(ct).ConfigureAwait(false))
                    {
                        existingTables.Add($"{reader.GetString(0)}.{reader.GetString(1)}");
                    }
                }

                await using (var colCmd = conn.CreateCommand())
                {
                    colCmd.CommandText = """
                        SELECT s.name AS SchemaName, t.name AS TableName, c.name AS ColumnName
                        FROM sys.columns c
                        INNER JOIN sys.tables t ON c.object_id = t.object_id
                        INNER JOIN sys.schemas s ON t.schema_id = s.schema_id;
                        """;
                    colCmd.CommandTimeout = 15;
                    await using var reader = await colCmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                    while (await reader.ReadAsync(ct).ConfigureAwait(false))
                    {
                        existingColumns.Add($"{reader.GetString(0)}.{reader.GetString(1)}.{reader.GetString(2)}");
                    }
                }

                await using (var idxCmd = conn.CreateCommand())
                {
                    idxCmd.CommandText = """
                        SELECT s.name AS SchemaName, t.name AS TableName, i.name AS IndexName
                        FROM sys.indexes i
                        INNER JOIN sys.tables t ON i.object_id = t.object_id
                        INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
                        WHERE i.name IS NOT NULL;
                        """;
                    idxCmd.CommandTimeout = 15;
                    await using var reader = await idxCmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                    while (await reader.ReadAsync(ct).ConfigureAwait(false))
                    {
                        existingIndexes.Add($"{reader.GetString(0)}.{reader.GetString(1)}.{reader.GetString(2)}");
                    }
                }
            }

            var allMissingObjects = new List<string>();
            var missingByContext = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var (name, factory) in contexts)
            {
                await using var dbContext = factory();
                var contextMissing = new List<string>();

                foreach (var entityType in dbContext.Model.GetEntityTypes())
                {
                    var tableName = entityType.GetTableName();
                    if (string.IsNullOrWhiteSpace(tableName))
                    {
                        continue;
                    }

                    var schemaName = entityType.GetSchema() ?? dbContext.Model.GetDefaultSchema() ?? "dbo";
                    var storeObject = Microsoft.EntityFrameworkCore.Metadata.StoreObjectIdentifier.Table(tableName, schemaName);
                    var qualifiedTable = $"{schemaName}.{tableName}";

                    if (!existingTables.Contains(qualifiedTable))
                    {
                        contextMissing.Add($"MissingTable:{qualifiedTable}");
                        continue;
                    }

                    foreach (var property in entityType.GetProperties())
                    {
                        var columnName = property.GetColumnName(storeObject);
                        if (string.IsNullOrWhiteSpace(columnName))
                        {
                            continue;
                        }

                        var qualifiedColumn = $"{qualifiedTable}.{columnName}";
                        if (!existingColumns.Contains(qualifiedColumn))
                        {
                            contextMissing.Add($"MissingColumn:{qualifiedColumn}");
                        }
                    }

                    foreach (var index in entityType.GetIndexes())
                    {
                        var indexName = index.GetDatabaseName(storeObject) ?? index.GetDatabaseName();
                        if (string.IsNullOrWhiteSpace(indexName))
                        {
                            continue;
                        }

                        var qualifiedIndex = $"{qualifiedTable}.{indexName}";
                        if (!existingIndexes.Contains(qualifiedIndex))
                        {
                            contextMissing.Add($"MissingIndex:{qualifiedIndex}");
                        }
                    }
                }

                if (contextMissing.Count > 0)
                {
                    var distinctMissing = contextMissing.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    allMissingObjects.AddRange(distinctMissing);
                    missingByContext[name] = distinctMissing;
                }
            }

            if (allMissingObjects.Count > 0)
            {
                logger?.LogWarning(
                    "Database migration history is current, but {Count} mapped schema objects are missing: {MissingObjects}",
                    allMissingObjects.Count,
                    string.Join(", ", allMissingObjects));

                return new SchemaCompatibilityResult(
                    SchemaCompatibilityStatus.DatabaseTooOld,
                    "The database schema is missing required tables, columns, or indexes. Required updates must be applied before normal application startup.",
                    $"Missing schema objects: {string.Join(", ", allMissingObjects)}",
                    allMissingObjects,
                    allUnknown,
                    missingByContext,
                    unknownByContext);
            }
        }
        catch (Exception ex)
        {
            var masked = DatabaseErrorMasker.Mask(ex.Message, connectionString);
            logger?.LogError(ex, "Failed verifying physical database catalog against EF Core models: {Error}", masked);
            return new SchemaCompatibilityResult(
                SchemaCompatibilityStatus.ConnectionFailed,
                "Failed to verify physical database schema objects.",
                masked);
        }

        logger?.LogInformation("Database schema compatibility verification passed. All migrations and physical schema objects match.");
        return new SchemaCompatibilityResult(
            SchemaCompatibilityStatus.Compatible,
            "The database schema is up-to-date and fully compatible.",
            null,
            allPending,
            allUnknown,
            pendingByContext,
            unknownByContext);
    }
}
