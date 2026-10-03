using System.Data;
using Clovent.Authentication.Infrastructure.Persistence;
using Clovent.Catalog.Infrastructure.Persistence;
using Clovent.Desktop.Configuration;
using Clovent.Identity.Infrastructure.Persistence;
using Clovent.Inventory.Infrastructure.Persistence;
using Clovent.MasterData.Infrastructure.Persistence;
using Clovent.Restaurant.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Clovent.Desktop.Commissioning.Database;

/// <summary>
/// Implements database provisioning, connection verification, initial database creation,
/// and sequential bounded-context migration deployment for commissioning and upgrade workflows.
/// </summary>
public sealed class DatabaseProvisioningService(ILogger<DatabaseProvisioningService>? logger = null) : IDatabaseProvisioningService
{
    /// <inheritdoc/>
    public async Task<(bool Success, string Message)> TestConnectionAsync(
        DatabaseConnectionSettings settings,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        string? plainPassword = settings.PlainTextPassword;
        if (string.IsNullOrEmpty(plainPassword) && !string.IsNullOrEmpty(settings.EncryptedPassword))
        {
            plainPassword = DatabaseSecretStore.Unprotect(settings.EncryptedPassword);
        }

        try
        {
            var testSettings = settings.Clone();
            testSettings.ConnectionTimeout = 10;
            var connectionString = testSettings.BuildConnectionString();

            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                ConnectTimeout = 10
            };

            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync(ct).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1;";
            command.CommandTimeout = 10;
            await command.ExecuteScalarAsync(ct).ConfigureAwait(false);

            return (true, "Connection test succeeded.");
        }
        catch (SqlException sqlEx)
        {
            logger?.LogWarning(sqlEx, "SQL Server connection test failed (Error {Number})", sqlEx.Number);
            var masked = DatabaseErrorMasker.Mask(sqlEx.Message, plainPassword);

            if (sqlEx.Number == 4060)
            {
                // Target database does not exist yet. Check if SQL Server instance is reachable via master.
                try
                {
                    var masterSettings = settings.Clone();
                    masterSettings.Database = "master";
                    masterSettings.ConnectionTimeout = 10;
                    var masterConnStr = masterSettings.BuildConnectionString();
                    await using var masterConn = new SqlConnection(masterConnStr);
                    await masterConn.OpenAsync(ct).ConfigureAwait(false);
                    return (true, $"Connected to SQL Server successfully. Database '{settings.Database}' does not exist yet and will be created during initialization in Step 3.");
                }
                catch (Exception masterEx)
                {
                    logger?.LogWarning(masterEx, "Master database check after error 4060 failed.");
                }
            }

            string userFriendly = sqlEx.Number switch
            {
                18456 => "Login failed: Invalid credentials or the specified user does not have permission.",
                4060 => $"Cannot open database '{settings.Database}': The database may not exist or the user lacks access.",
                53 or 258 => $"Unable to reach SQL Server at '{settings.Server}'. Please verify the server name, network connectivity, and that SQL Server is running.",
                _ => $"SQL Server connection failed: {masked}"
            };

            return (false, userFriendly);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Unexpected error during connection test");
            var masked = DatabaseErrorMasker.Mask(ex.Message, plainPassword);
            return (false, $"Connection test failed: {masked}");
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DatabaseExistsAsync(
        DatabaseConnectionSettings settings,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var targetDb = string.IsNullOrWhiteSpace(settings.Database)
            ? DatabaseConnectionSettings.DefaultDatabaseName
            : settings.Database.Trim();

        // 1. Query sys.databases on the master database
        try
        {
            var masterSettings = settings.Clone();
            masterSettings.Database = "master";
            masterSettings.ConnectionTimeout = 10;

            var builder = new SqlConnectionStringBuilder(masterSettings.BuildConnectionString())
            {
                ConnectTimeout = 10
            };

            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync(ct).ConfigureAwait(false);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(1) FROM sys.databases WHERE name = @dbName;";
            command.Parameters.Add(new SqlParameter("@dbName", SqlDbType.NVarChar, 128) { Value = targetDb });
            command.CommandTimeout = 10;

            var result = await command.ExecuteScalarAsync(ct).ConfigureAwait(false);
            return Convert.ToInt32(result) > 0;
        }
        catch (Exception masterEx)
        {
            logger?.LogDebug(masterEx, "Direct sys.databases query on master failed; falling back to direct database probe.");

            // 2. Fallback: Attempt a direct connection to the target database
            try
            {
                var targetSettings = settings.Clone();
                targetSettings.Database = targetDb;
                targetSettings.ConnectionTimeout = 5;

                var directBuilder = new SqlConnectionStringBuilder(targetSettings.BuildConnectionString())
                {
                    ConnectTimeout = 5
                };

                await using var directConn = new SqlConnection(directBuilder.ConnectionString);
                await directConn.OpenAsync(ct).ConfigureAwait(false);
                return true;
            }
            catch (SqlException sqlEx) when (sqlEx.Number == 4060)
            {
                // Error 4060: Cannot open database requested by the login
                return false;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <inheritdoc/>
    public async Task<(bool Success, string Message)> CreateDatabaseAsync(
        DatabaseConnectionSettings settings,
        string? adminUserId = null,
        string? adminPassword = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var targetDb = string.IsNullOrWhiteSpace(settings.Database)
            ? DatabaseConnectionSettings.DefaultDatabaseName
            : settings.Database.Trim();

        // Check if database already exists
        if (await DatabaseExistsAsync(settings, ct).ConfigureAwait(false))
        {
            return (true, $"Database '{targetDb}' already exists.");
        }

        // Configure connection pointing to master
        var masterSettings = settings.Clone();
        masterSettings.Database = "master";
        masterSettings.ConnectionTimeout = 15;

        if (!string.IsNullOrWhiteSpace(adminUserId))
        {
            masterSettings.UseWindowsAuthentication = false;
            masterSettings.UserId = adminUserId.Trim();
            masterSettings.PlainTextPassword = adminPassword;
            masterSettings.EncryptedPassword = null;
        }

        var masterCs = masterSettings.BuildConnectionString();
        var builder = new SqlConnectionStringBuilder(masterCs)
        {
            ConnectTimeout = 15
        };

        var effectivePassword = adminPassword ?? masterSettings.PlainTextPassword;

        try
        {
            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync(ct).ConfigureAwait(false);

            var escapedDb = targetDb.Replace("]", "]]");
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE [{escapedDb}];";
            command.CommandTimeout = 120;
            await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

            logger?.LogInformation("Successfully created database '{DatabaseName}' on server '{Server}'", targetDb, settings.Server);
            return (true, $"Database '{targetDb}' was created successfully.");
        }
        catch (SqlException sqlEx) when (sqlEx.Number == 1801)
        {
            // Database already exists
            return (true, $"Database '{targetDb}' already exists.");
        }
        catch (SqlException sqlEx) when (sqlEx.Number == 262 || sqlEx.Message.Contains("permission denied", StringComparison.OrdinalIgnoreCase))
        {
            logger?.LogWarning(sqlEx, "Permission denied creating database '{DatabaseName}'", targetDb);
            return (false, $"Permission denied to create database '{targetDb}'. Your login lacks the 'CREATE DATABASE' or 'dbcreator' permission on the server. A Database Administrator (DBA) can create an empty database named '{targetDb}' and grant your user account 'db_owner' permissions, or provide administrator credentials in the installer.");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed creating database '{DatabaseName}'", targetDb);
            var masked = DatabaseErrorMasker.Mask(ex.Message, effectivePassword);
            return (false, $"Failed to create database '{targetDb}': {masked}");
        }
    }

    /// <inheritdoc/>
    public async Task<(bool Success, string Message)> ApplyMigrationsAsync(
        string connectionString,
        Action<string, int>? progressCallback = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        try
        {
            // 1. AuthenticationDbContext
            progressCallback?.Invoke("Applying Authentication migrations...", 10);
            logger?.LogInformation("Applying migrations for AuthenticationDbContext...");
            await using (var authContext = new AuthenticationDbContext(
                new DbContextOptionsBuilder<AuthenticationDbContext>()
                    .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "Authentication"))
                    .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
                    .Options))
            {
                var authInit = new AuthenticationPersistenceInitializer(authContext);
                await authInit.InitializeAsync(ct).ConfigureAwait(false);
            }

            // 2. IdentityDbContext
            progressCallback?.Invoke("Applying Identity migrations...", 25);
            logger?.LogInformation("Applying migrations for IdentityDbContext...");
            await using (var identityContext = new IdentityDbContext(
                new DbContextOptionsBuilder<IdentityDbContext>()
                    .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "Identity"))
                    .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
                    .Options))
            {
                var identityInit = new IdentityPersistenceInitializer(identityContext);
                await identityInit.InitializeAsync(ct).ConfigureAwait(false);
            }

            // 3. MasterDataDbContext
            progressCallback?.Invoke("Applying Master Data migrations...", 40);
            logger?.LogInformation("Applying migrations for MasterDataDbContext...");
            await using (var masterDataContext = new MasterDataDbContext(
                new DbContextOptionsBuilder<MasterDataDbContext>()
                    .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "MasterData"))
                    .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
                    .Options))
            {
                var masterDataInit = new MasterDataPersistenceInitializer(masterDataContext);
                await masterDataInit.InitializeAsync(ct).ConfigureAwait(false);
            }

            // 4. CatalogDbContext
            progressCallback?.Invoke("Applying Catalog migrations and schema...", 55);
            logger?.LogInformation("Applying migrations for CatalogDbContext...");
            await using (var catalogContext = new CatalogDbContext(
                new DbContextOptionsBuilder<CatalogDbContext>()
                    .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "Catalog"))
                    .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
                    .Options))
            {
                var catalogInit = new CatalogPersistenceInitializer(catalogContext);
                await catalogInit.InitializeAsync(ct).ConfigureAwait(false);
            }

            // 5. InventoryDbContext
            progressCallback?.Invoke("Applying Inventory migrations...", 70);
            logger?.LogInformation("Applying migrations for InventoryDbContext...");
            await using (var inventoryContext = new InventoryDbContext(
                new DbContextOptionsBuilder<InventoryDbContext>()
                    .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "Inventory"))
                    .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
                    .Options))
            {
                var inventoryInit = new InventoryPersistenceInitializer(inventoryContext);
                await inventoryInit.InitializeAsync(ct).ConfigureAwait(false);
            }

            // 6. RestaurantDbContext & DDL
            progressCallback?.Invoke("Applying Restaurant migrations & schema...", 85);
            logger?.LogInformation("Applying migrations and schema initializers for RestaurantDbContext...");
            await using (var restaurantContext = new RestaurantDbContext(
                new DbContextOptionsBuilder<RestaurantDbContext>()
                    .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "Restaurant"))
                    .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
                    .Options))
            {
                var restaurantInit = new RestaurantPersistenceInitializer(restaurantContext);
                await restaurantInit.InitializeAsync(ct).ConfigureAwait(false);
            }

            progressCallback?.Invoke("Database migrations completed successfully.", 100);
            logger?.LogInformation("All bounded context migrations and schema initializations applied successfully.");
            return (true, "All database migrations and schemas were applied successfully.");
        }
        catch (Exception ex)
        {
            var masked = DatabaseErrorMasker.Mask(ex.Message, connectionString);
            logger?.LogError(ex, "Database migration failed: {Message}", masked);
            return (false, $"Database migration failed: {masked}");
        }
    }
}
