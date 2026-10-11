using Clovent.Desktop.Commissioning.Database;
using Clovent.Desktop.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Clovent.Desktop.Tests.Commissioning;

public sealed class DatabaseCommissioningTests
{
    [Fact]
    public void DatabaseErrorMasker_MasksPlainTextPassword_WhenPresentInMessage()
    {
        // Arrange
        const string rawMessage = "Failed to connect using user 'sa' and password 'SuperSecret123!'.";
        const string secretPassword = "SuperSecret123!";

        // Act
        var result = DatabaseErrorMasker.Mask(rawMessage, secretPassword);

        // Assert
        Assert.DoesNotContain(secretPassword, result);
        Assert.Contains("******", result);
    }

    [Fact]
    public void DatabaseErrorMasker_MasksConnectionStringPasswords()
    {
        // Arrange
        const string cs = "Server=localhost;Database=TestDb;User Id=dbadmin;Password=UltraClassified!2026;TrustServerCertificate=True;";
        const string rawMessage = $"Connection failed: {cs}";

        // Act
        var result = DatabaseErrorMasker.Mask(rawMessage, cs);

        // Assert
        Assert.DoesNotContain("UltraClassified!2026", result);
        Assert.Contains("Password=******", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DatabaseErrorMasker_HandlesNullAndEmptyInput()
    {
        Assert.Equal(string.Empty, DatabaseErrorMasker.Mask(null));
        Assert.Equal(string.Empty, DatabaseErrorMasker.Mask(string.Empty));
        Assert.Equal(string.Empty, DatabaseErrorMasker.Mask("   "));
    }

    [Fact]
    public async Task DatabaseSchemaCompatibilityValidator_ReturnsConnectionFailed_OnEmptyConnectionString()
    {
        // Arrange
        var validator = new DatabaseSchemaCompatibilityValidator();

        // Act
        var result = await validator.ValidateCompatibilityAsync(string.Empty);

        // Assert
        Assert.Equal(SchemaCompatibilityStatus.ConnectionFailed, result.Status);
        Assert.False(result.IsCompatible);
        Assert.Contains("No database connection string", result.Message);
    }

    [Fact]
    public async Task DatabaseSchemaCompatibilityValidator_ReturnsConnectionFailed_WhenServerUnreachable()
    {
        // Arrange
        var validator = new DatabaseSchemaCompatibilityValidator();
        const string invalidCs = "Server=127.0.0.1,65534;Database=NonExistentDb_12345;Connection Timeout=1;TrustServerCertificate=True;";

        // Act
        var result = await validator.ValidateCompatibilityAsync(invalidCs);

        // Assert
        Assert.Equal(SchemaCompatibilityStatus.ConnectionFailed, result.Status);
        Assert.False(result.IsCompatible);
        Assert.NotNull(result.Message);
        Assert.DoesNotContain("Password", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SchemaCompatibilityResult_IsCompatible_ReflectsStatusCorrectly()
    {
        var compatible = new SchemaCompatibilityResult(SchemaCompatibilityStatus.Compatible, "Compatible");
        var tooOld = new SchemaCompatibilityResult(SchemaCompatibilityStatus.DatabaseTooOld, "Too Old");
        var newer = new SchemaCompatibilityResult(SchemaCompatibilityStatus.DatabaseNewer, "Newer");
        var failed = new SchemaCompatibilityResult(SchemaCompatibilityStatus.ConnectionFailed, "Failed");

        Assert.True(compatible.IsCompatible);
        Assert.False(tooOld.IsCompatible);
        Assert.False(newer.IsCompatible);
        Assert.False(failed.IsCompatible);
    }

    [Fact]
    public async Task DatabaseBackupService_ReturnsError_WhenConnectionStringEmpty()
    {
        // Arrange
        var backupService = new DatabaseBackupService();

        // Act
        var (success, backupPath, message) = await backupService.CreateBackupAsync(string.Empty);

        // Assert
        Assert.False(success);
        Assert.Equal(string.Empty, backupPath);
        Assert.Contains("empty", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DatabaseBackupService_FailsSafely_WhenTargetDirectoryInvalid()
    {
        // Arrange
        var backupService = new DatabaseBackupService();
        const string invalidCs = "Server=127.0.0.1,65534;Database=TestDb;Connection Timeout=1;TrustServerCertificate=True;";

        // Act
        var (success, backupPath, message) = await backupService.CreateBackupAsync(invalidCs, "Z:\\NonExistentDrive_XYZ123\\Backups");

        // Assert
        Assert.False(success);
        Assert.Equal(string.Empty, backupPath);
        Assert.NotNull(message);
    }

    [Fact]
    public async Task DatabaseProvisioningService_ThrowsArgumentNullException_OnNullSettings()
    {
        var service = new DatabaseProvisioningService();

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.TestConnectionAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.DatabaseExistsAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.CreateDatabaseAsync(null!));
    }

    [Fact]
    public async Task DatabaseProvisioningService_TestConnectionAsync_MasksPasswordOnFailure()
    {
        // Arrange
        var service = new DatabaseProvisioningService();
        var settings = new DatabaseConnectionSettings
        {
            Server = "127.0.0.1,65534",
            Database = "Clovent_BusinessOperatingSystem",
            UseWindowsAuthentication = false,
            UserId = "testuser",
            PlainTextPassword = "SecretPlainPassword123!",
            ConnectionTimeout = 1
        };

        // Act
        var (success, message) = await service.TestConnectionAsync(settings);

        // Assert
        Assert.False(success);
        Assert.DoesNotContain("SecretPlainPassword123!", message);
    }

    [Fact]
    public async Task DatabaseProvisioningService_ApplyMigrationsAsync_ThrowsOnEmptyConnectionString()
    {
        var service = new DatabaseProvisioningService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.ApplyMigrationsAsync(string.Empty));
        await Assert.ThrowsAsync<ArgumentException>(() => service.ApplyMigrationsAsync("   "));
    }

    [Theory]
    [InlineData("--unknown-flag")]
    [InlineData("--server")]
    [InlineData("--database")]
    [InlineData("--auth", "bogus")]
    [InlineData("--auth", "sql")]
    [InlineData("--auth", "sql", "--user", "sa")]
    [InlineData("--auth", "windows", "--password", "Secret123!")]
    [InlineData("--provision", "--verify-only")]
    public async Task ProvisionerCli_InvalidArguments_FailsBeforeSideEffectsWithExitCode64(params string[] invalidArgs)
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var debugDll = Path.Combine(repoRoot, "tools", "Clovent.Installer.Provisioner", "bin", "Debug", "net10.0-windows", "win-x64", "Clovent.Installer.Provisioner.dll");
        var releaseDll = Path.Combine(repoRoot, "tools", "Clovent.Installer.Provisioner", "bin", "Release", "net10.0-windows", "win-x64", "Clovent.Installer.Provisioner.dll");
        var targetDll = File.Exists(debugDll) ? debugDll : releaseDll;
        Assert.True(File.Exists(targetDll), $"Expected compiled provisioner assembly at '{debugDll}' or '{releaseDll}'.");

        var tempLogPath = Path.Combine(Path.GetTempPath(), $"cbos-prov-should-not-exist-{Guid.NewGuid():N}.log");
        var argsWithLog = invalidArgs.Concat(["--log-file", tempLogPath]).ToArray();

        try
        {
            var asm = System.Reflection.Assembly.LoadFrom(targetDll);
            var programType = asm.GetType("Clovent.Installer.Provisioner.Program", throwOnError: true)!;
            var mainMethod = programType.GetMethod("Main", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!;

            var exitCode = await (Task<int>)mainMethod.Invoke(null, [argsWithLog])!;
            Assert.Equal(64, exitCode);
            Assert.False(File.Exists(tempLogPath), "Provisioner must fail before creating log files or touching disk when arguments are invalid.");
        }
        finally
        {
            if (File.Exists(tempLogPath))
            {
                File.Delete(tempLogPath);
            }
        }
    }

    [Fact]
    public async Task FreshInstall_DisposableSqlDatabase_CreatesAllMappedTablesColumnsIndexesAndOutboxDependencies()
    {
        var dbName = $"CBOS_Disposable_Fresh_{Guid.NewGuid():N}";
        var settings = new DatabaseConnectionSettings
        {
            Server = ".",
            Database = dbName,
            UseWindowsAuthentication = true,
            TrustServerCertificate = true,
            ConnectionTimeout = 15
        };

        var provisioningService = new DatabaseProvisioningService();
        var validator = new DatabaseSchemaCompatibilityValidator();
        var connectionString = settings.BuildConnectionString();
        Clovent.Desktop.Tests.TestSupport.DatabaseIsolationGuard.EnsureIsolated(connectionString);

        try
        {
            var (created, createMsg) = await provisioningService.CreateDatabaseAsync(settings);
            Assert.True(created, createMsg);

            var (migrated, migrateMsg) = await provisioningService.ApplyMigrationsAsync(connectionString);
            Assert.True(migrated, migrateMsg);

            var validation = await validator.ValidateCompatibilityAsync(connectionString);
            Assert.Equal(SchemaCompatibilityStatus.Compatible, validation.Status);
            Assert.True(validation.IsCompatible, validation.Message);
            Assert.Empty(validation.PendingMigrations);
            Assert.Empty(validation.UnknownMigrations);

            // Verify critical schema objects and outbox dependencies directly in SQL Server
            await using var conn = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
            await conn.OpenAsync();

            static async Task<int> ScalarIntAsync(Microsoft.Data.SqlClient.SqlConnection c, string sql)
            {
                await using var cmd = c.CreateCommand();
                cmd.CommandText = sql;
                return Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            Assert.Equal(1, await ScalarIntAsync(conn, "SELECT COUNT(1) FROM sys.columns WHERE object_id = OBJECT_ID(N'[Catalog].[ProductCategories]') AND name = N'DefaultTaxProfileId';"));
            Assert.Equal(1, await ScalarIntAsync(conn, "SELECT COUNT(1) FROM sys.columns WHERE object_id = OBJECT_ID(N'[Catalog].[Products]') AND name = N'ItemType';"));
            Assert.Equal(1, await ScalarIntAsync(conn, "SELECT COUNT(1) FROM sys.columns WHERE object_id = OBJECT_ID(N'[Catalog].[ProductVariants]') AND name = N'IsAvailable';"));
            Assert.Equal(1, await ScalarIntAsync(conn, "SELECT COUNT(1) FROM sys.tables WHERE object_id = OBJECT_ID(N'[Restaurant].[QuickBooksSyncMaps]');"));
            Assert.Equal(1, await ScalarIntAsync(conn, "SELECT COUNT(1) FROM sys.indexes WHERE object_id = OBJECT_ID(N'[Restaurant].[QuickBooksSyncMaps]') AND name = N'IX_QuickBooksSyncMaps_EntityType_LocalEntityId';"));
            Assert.Equal(1, await ScalarIntAsync(conn, "SELECT COUNT(1) FROM sys.tables WHERE object_id = OBJECT_ID(N'[Restaurant].[SyncInboxRecords]');"));
            Assert.Equal(1, await ScalarIntAsync(conn, "SELECT COUNT(1) FROM sys.tables WHERE object_id = OBJECT_ID(N'[Restaurant].[SyncConflicts]');"));
            Assert.Equal(1, await ScalarIntAsync(conn, "SELECT COUNT(1) FROM sys.tables WHERE object_id = OBJECT_ID(N'[Restaurant].[TerminalShiftSyncSummaries]');"));
            Assert.Equal(1, await ScalarIntAsync(conn, "SELECT COUNT(1) FROM sys.tables WHERE object_id = OBJECT_ID(N'[Restaurant].[CashierAuditAlerts]');"));
            Assert.Equal(1, await ScalarIntAsync(conn, "SELECT COUNT(1) FROM sys.indexes WHERE object_id = OBJECT_ID(N'[Inventory].[InventoryTransactions]') AND name = N'IX_InventoryTransactions_Reference_Deduplication';"));
        }
        finally
        {
            await DropDisposableDatabaseAsync(dbName);
        }
    }

    [Fact]
    public async Task SupportedUpgradeAndDriftDetection_DisposableSqlDatabase_DetectsMissingColumnAndTableAndUpgradesCleanly()
    {
        var dbName = $"CBOS_Disposable_Upgrade_{Guid.NewGuid():N}";
        var settings = new DatabaseConnectionSettings
        {
            Server = ".",
            Database = dbName,
            UseWindowsAuthentication = true,
            TrustServerCertificate = true,
            ConnectionTimeout = 15
        };

        var provisioningService = new DatabaseProvisioningService();
        var validator = new DatabaseSchemaCompatibilityValidator();
        var connectionString = settings.BuildConnectionString();
        Clovent.Desktop.Tests.TestSupport.DatabaseIsolationGuard.EnsureIsolated(connectionString);

        try
        {
            var (created, createMsg) = await provisioningService.CreateDatabaseAsync(settings);
            Assert.True(created, createMsg);

            var (migrated, migrateMsg) = await provisioningService.ApplyMigrationsAsync(connectionString);
            Assert.True(migrated, migrateMsg);

            // 1. Simulate the exact schema drift where __EFMigrationsHistory claims all migrations are applied,
            //    but Catalog.ProductCategories.DefaultTaxProfileId and Restaurant.QuickBooksSyncMaps are missing.
            await using (var conn = new Microsoft.Data.SqlClient.SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = """
                    ALTER TABLE [Catalog].[ProductCategories] DROP COLUMN [DefaultTaxProfileId];
                    DROP TABLE [Restaurant].[QuickBooksSyncMaps];
                    """;
                await cmd.ExecuteNonQueryAsync();
            }

            // Validator must NOT report Compatible when physical table/column is missing even if __EFMigrationsHistory has all rows
            var driftedResult = await validator.ValidateCompatibilityAsync(connectionString);
            Assert.Equal(SchemaCompatibilityStatus.DatabaseTooOld, driftedResult.Status);
            Assert.False(driftedResult.IsCompatible);
            Assert.Contains("MissingColumn:Catalog.ProductCategories.DefaultTaxProfileId", driftedResult.PendingMigrations);
            Assert.Contains("MissingTable:Restaurant.QuickBooksSyncMaps", driftedResult.PendingMigrations);

            // 2. Simulate a pre-upgrade database from before the additive migrations (where 20261008151934_AddTaxProfiles was applied
            //    prior to DefaultTaxProfileId being added, and before QuickBooksSyncMaps migration existed)
            await using (var conn = new Microsoft.Data.SqlClient.SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = """
                    DELETE FROM [Catalog].[__EFMigrationsHistory] WHERE [MigrationId] = N'20261011140000_EnsureCatalogSchemaColumns';
                    DELETE FROM [Restaurant].[__EFMigrationsHistory] WHERE [MigrationId] = N'20261011140500_AddQuickBooksSyncMapsAndSyncAuditTables';
                    DROP TABLE IF EXISTS [Restaurant].[SyncInboxRecords];
                    DROP TABLE IF EXISTS [Restaurant].[SyncConflicts];
                    DROP TABLE IF EXISTS [Restaurant].[TerminalShiftSyncSummaries];
                    DROP TABLE IF EXISTS [Restaurant].[CashierAuditAlerts];
                    """;
                await cmd.ExecuteNonQueryAsync();
            }

            var preUpgradeResult = await validator.ValidateCompatibilityAsync(connectionString);
            Assert.Equal(SchemaCompatibilityStatus.DatabaseTooOld, preUpgradeResult.Status);

            // 3. Run supported upgrade path without manual SQL patches
            var (upgraded, upgradeMsg) = await provisioningService.ApplyMigrationsAsync(connectionString);
            Assert.True(upgraded, upgradeMsg);

            var postUpgradeResult = await validator.ValidateCompatibilityAsync(connectionString);
            Assert.Equal(SchemaCompatibilityStatus.Compatible, postUpgradeResult.Status);
            Assert.True(postUpgradeResult.IsCompatible, postUpgradeResult.Message);
        }
        finally
        {
            await DropDisposableDatabaseAsync(dbName);
        }
    }

    [Theory]
    [InlineData(false)] // Prior Variant A (commit 426be69): 20261008151934_AddTaxProfiles without DefaultTaxProfileId/ItemType/IsAvailable; Restaurant at 20261008152015_AddRefundDomainAndTaxSnapshots
    [InlineData(true)]  // Prior Variant B (commit 4b1ef15/63aebaf): 20261008151934_AddTaxProfiles with DefaultTaxProfileId; Restaurant at 20261011050000_AddRefundShiftAndTerminalAttribution without QuickBooksSyncMaps
    public async Task ActualPriorVersionUpgrade_WithSyntheticData_PreservesDataAndSupportsRepeatProvisioning(
        bool priorVariantIncludesDefaultTaxProfileId)
    {
        var dbName = $"CBOS_Disposable_PriorVer_{Guid.NewGuid():N}";
        var settings = new DatabaseConnectionSettings
        {
            Server = ".",
            Database = dbName,
            UseWindowsAuthentication = true,
            TrustServerCertificate = true,
            ConnectionTimeout = 15
        };

        var provisioningService = new DatabaseProvisioningService();
        var validator = new DatabaseSchemaCompatibilityValidator();
        var connectionString = settings.BuildConnectionString();
        Clovent.Desktop.Tests.TestSupport.DatabaseIsolationGuard.EnsureIsolated(connectionString);

        var branchId = Guid.NewGuid();
        var wh01Id = Guid.NewGuid();
        var wh02Id = Guid.NewGuid();
        var uomId = Guid.NewGuid();
        var taxProfileId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var stockTransferId = Guid.NewGuid();
        var txOutId = Guid.NewGuid();
        var txInId = Guid.NewGuid();
        var outboxMsgId = Guid.NewGuid();

        try
        {
            var (created, createMsg) = await provisioningService.CreateDatabaseAsync(settings);
            Assert.True(created, createMsg);

            // 1. Provision the disposable database to the exact prior released/candidate migration targets
            await using (var authDb = new Clovent.Authentication.Infrastructure.Persistence.AuthenticationDbContext(
                new DbContextOptionsBuilder<Clovent.Authentication.Infrastructure.Persistence.AuthenticationDbContext>()
                    .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "Authentication"))
                    .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
                    .Options))
            {
                var migrator = authDb.Database.GetService<IMigrator>();
                await migrator.MigrateAsync("20260727115243_InitialCreate");
            }

            await using (var idDb = new Clovent.Identity.Infrastructure.Persistence.IdentityDbContext(
                new DbContextOptionsBuilder<Clovent.Identity.Infrastructure.Persistence.IdentityDbContext>()
                    .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "Identity"))
                    .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
                    .Options))
            {
                var migrator = idDb.Database.GetService<IMigrator>();
                await migrator.MigrateAsync("20260729051438_AddUserCompanyBranch");
            }

            await using (var mdDb = new Clovent.MasterData.Infrastructure.Persistence.MasterDataDbContext(
                new DbContextOptionsBuilder<Clovent.MasterData.Infrastructure.Persistence.MasterDataDbContext>()
                    .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "MasterData"))
                    .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
                    .Options))
            {
                var migrator = mdDb.Database.GetService<IMigrator>();
                await migrator.MigrateAsync("20260727162427_InitialCreate");
            }

            await using (var catDb = new Clovent.Catalog.Infrastructure.Persistence.CatalogDbContext(
                new DbContextOptionsBuilder<Clovent.Catalog.Infrastructure.Persistence.CatalogDbContext>()
                    .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "Catalog"))
                    .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
                    .Options))
            {
                var migrator = catDb.Database.GetService<IMigrator>();
                if (!priorVariantIncludesDefaultTaxProfileId)
                {
                    await migrator.MigrateAsync("20260821123728_UpdateMonetaryDecimalPrecision");
                    // Apply exact commit 426be69 definition of 20261008151934_AddTaxProfiles (without DefaultTaxProfileId)
                    await catDb.Database.ExecuteSqlRawAsync("""
                        CREATE TABLE [Catalog].[TaxProfiles] (
                            [Id] uniqueidentifier NOT NULL,
                            [Code] nvarchar(50) NOT NULL,
                            [InvoiceDisplayName] nvarchar(150) NOT NULL,
                            [Authority] nvarchar(50) NOT NULL,
                            [Jurisdiction] nvarchar(50) NOT NULL,
                            [TaxClassification] nvarchar(30) NOT NULL,
                            [ItemClassification] nvarchar(30) NOT NULL,
                            [RatePercentage] decimal(18,4) NOT NULL,
                            [PricingMode] nvarchar(20) NOT NULL,
                            [EffectiveFromUtc] datetimeoffset NOT NULL,
                            [EffectiveToUtc] datetimeoffset NULL,
                            [IsActive] bit NOT NULL,
                            [ReferenceDocument] nvarchar(255) NULL,
                            [PaymentMethodRestriction] nvarchar(100) NULL,
                            [Notes] nvarchar(500) NULL,
                            [CreatedAtUtc] datetimeoffset NOT NULL,
                            [UpdatedAtUtc] datetimeoffset NOT NULL,
                            CONSTRAINT [PK_TaxProfiles] PRIMARY KEY ([Id])
                        );
                        CREATE UNIQUE INDEX [IX_TaxProfiles_Code] ON [Catalog].[TaxProfiles] ([Code]);
                        INSERT INTO [Catalog].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                        VALUES (N'20261008151934_AddTaxProfiles', N'10.0.5');
                        """);
                }
                else
                {
                    await migrator.MigrateAsync("20261008151934_AddTaxProfiles");
                    await catDb.Database.ExecuteSqlRawAsync("""
                        ALTER TABLE [Catalog].[Products] ADD [ItemType] nvarchar(30) NOT NULL CONSTRAINT [DF_Products_ItemType] DEFAULT N'Prepared';
                        ALTER TABLE [Catalog].[ProductVariants] ADD [IsAvailable] bit NOT NULL CONSTRAINT [DF_ProductVariants_IsAvailable] DEFAULT 1;
                        """);
                }
            }

            await using (var invDb = new Clovent.Inventory.Infrastructure.Persistence.InventoryDbContext(
                new DbContextOptionsBuilder<Clovent.Inventory.Infrastructure.Persistence.InventoryDbContext>()
                    .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "Inventory"))
                    .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
                    .Options))
            {
                var migrator = invDb.Database.GetService<IMigrator>();
                var targetInvMig = priorVariantIncludesDefaultTaxProfileId
                    ? "20261011063000_AddInventoryTransactionReferenceDeduplicationIndex"
                    : "20260727231859_InitialCreate";
                await migrator.MigrateAsync(targetInvMig);
            }

            await using (var restDb = new Clovent.Restaurant.Infrastructure.Persistence.RestaurantDbContext(
                new DbContextOptionsBuilder<Clovent.Restaurant.Infrastructure.Persistence.RestaurantDbContext>()
                    .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "Restaurant"))
                    .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
                    .Options))
            {
                var migrator = restDb.Database.GetService<IMigrator>();
                var targetRestMig = priorVariantIncludesDefaultTaxProfileId
                    ? "20261011050000_AddRefundShiftAndTerminalAttribution"
                    : "20261008152015_AddRefundDomainAndTaxSnapshots";
                await migrator.MigrateAsync(targetRestMig);
            }

            // 2. Seed representative synthetic data into the prior-version database before upgrade
            await using (var conn = new Microsoft.Data.SqlClient.SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                await using var seedCmd = conn.CreateCommand();
                seedCmd.CommandText = $"""
                    INSERT INTO [MasterData].[Warehouses] ([Id], [BranchId], [Name], [Code], [Status], [CreatedAtUtc])
                    VALUES
                        ('{wh01Id}', '{branchId}', N'Main Kitchen Warehouse', N'WH-01', N'Active', SYSDATETIMEOFFSET()),
                        ('{wh02Id}', '{branchId}', N'Branch 2 Warehouse', N'WH-02', N'Active', SYSDATETIMEOFFSET());

                    INSERT INTO [Catalog].[UnitsOfMeasure] ([Id], [Code], [Name], [Status], [CreatedAtUtc])
                    VALUES ('{uomId}', N'PCS', N'Pieces', N'Active', SYSDATETIMEOFFSET());

                    INSERT INTO [Catalog].[TaxProfiles] ([Id], [Code], [InvoiceDisplayName], [Authority], [Jurisdiction], [TaxClassification], [ItemClassification], [RatePercentage], [PricingMode], [EffectiveFromUtc], [EffectiveToUtc], [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
                    VALUES ('{taxProfileId}', N'GST-16', N'GST 16%', N'PRA', N'PK-PB', N'Standard', N'PreparedFood', 16.0000, N'TaxExclusive', SYSDATETIMEOFFSET(), NULL, 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());

                    INSERT INTO [Catalog].[ProductCategories] ([Id], [Name], [ParentCategoryId], [Status], [CreatedAtUtc], [ColorHex], [SortOrder])
                    VALUES ('{categoryId}', N'Main Course', NULL, N'Active', SYSDATETIMEOFFSET(), N'#FF5722', 1);

                    INSERT INTO [Catalog].[Products] ([Id], [Name], [Sku], [CategoryId], [GroupId], [BrandId], [BaseUnitOfMeasureId], [TaxConfiguration], [Status], [CreatedAtUtc])
                    VALUES ('{productId}', N'Chicken Karahi', N'SKU-KARAHI', '{categoryId}', NULL, NULL, '{uomId}', N'Standard', N'Active', SYSDATETIMEOFFSET());

                    INSERT INTO [Catalog].[ProductVariants] ([Id], [ProductId], [Name], [Sku], [UnitOfMeasureId], [Status], [CreatedAtUtc])
                    VALUES ('{variantId}', '{productId}', N'Chicken Karahi (Full)', N'SKU-KARAHI-FULL', '{uomId}', N'Active', SYSDATETIMEOFFSET());

                    INSERT INTO [Inventory].[StockTransfers] ([Id], [SourceWarehouseId], [DestinationWarehouseId], [ProductVariantId], [Quantity], [Status], [CreatedAtUtc], [CompletedAtUtc])
                    VALUES ('{stockTransferId}', '{wh01Id}', '{wh02Id}', '{variantId}', 10.0000, N'Completed', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());

                    INSERT INTO [Inventory].[InventoryTransactions] ([Id], [WarehouseId], [ProductVariantId], [TransactionType], [Quantity], [ReferenceType], [ReferenceId], [Notes], [OccurredAtUtc])
                    VALUES
                        ('{txOutId}', '{wh01Id}', '{variantId}', N'TransferOut', -10.0000, N'StockTransfer', '{stockTransferId}', N'Seed transfer out', 1000001),
                        ('{txInId}', '{wh02Id}', '{variantId}', N'TransferIn', 10.0000, N'StockTransfer', '{stockTransferId}', N'Seed transfer in', 1000002);

                    INSERT INTO [Restaurant].[OutboxMessages] ([Id], [MessageType], [AggregateType], [AggregateId], [CorrelationId], [IdempotencyKey], [Payload], [Status], [AttemptCount], [CreatedAtUtc], [AvailableAtUtc], [Priority], [Version])
                    VALUES ('{outboxMsgId}', N'QuickBooksSync', N'Order', N'{Guid.NewGuid()}', N'corr-prior-1', N'idem-prior-1', N'{"{"}"OrderNumber":"ORD-PRIOR-001","TotalAmount":1500.00{"}"}', N'Pending', 0, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET(), 0, 0x01020304);
                    """;
                await seedCmd.ExecuteNonQueryAsync();
            }

            // 3. Validator must detect the prior-version database as DatabaseTooOld before upgrade
            var preUpgradeValidation = await validator.ValidateCompatibilityAsync(connectionString);
            Assert.Equal(SchemaCompatibilityStatus.DatabaseTooOld, preUpgradeValidation.Status);
            Assert.False(preUpgradeValidation.IsCompatible);

            // 4. Execute supported upgrade via DatabaseProvisioningService
            var (upgraded, upgradeMsg) = await provisioningService.ApplyMigrationsAsync(connectionString);
            Assert.True(upgraded, upgradeMsg);

            var postUpgradeValidation = await validator.ValidateCompatibilityAsync(connectionString);
            Assert.Equal(SchemaCompatibilityStatus.Compatible, postUpgradeValidation.Status);
            Assert.True(postUpgradeValidation.IsCompatible, postUpgradeValidation.Message);

            // 5. Verify 100% data preservation and new schema defaults/tables
            await using (var conn = new Microsoft.Data.SqlClient.SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                static async Task<T> ScalarAsync<T>(Microsoft.Data.SqlClient.SqlConnection c, string sql)
                {
                    await using var cmd = c.CreateCommand();
                    cmd.CommandText = sql;
                    var val = await cmd.ExecuteScalarAsync();
                    return (T)Convert.ChangeType(val!, typeof(T));
                }

                Assert.Equal(2, await ScalarAsync<int>(conn, "SELECT COUNT(1) FROM [MasterData].[Warehouses];"));
                Assert.Equal(1, await ScalarAsync<int>(conn, $"SELECT COUNT(1) FROM [Catalog].[TaxProfiles] WHERE [Id] = '{taxProfileId}' AND [Code] = N'GST-16';"));
                Assert.Equal(1, await ScalarAsync<int>(conn, $"SELECT COUNT(1) FROM [Catalog].[ProductCategories] WHERE [Id] = '{categoryId}' AND [Name] = N'Main Course' AND [DefaultTaxProfileId] IS NULL;"));
                Assert.Equal(1, await ScalarAsync<int>(conn, $"SELECT COUNT(1) FROM [Catalog].[Products] WHERE [Id] = '{productId}' AND [Sku] = N'SKU-KARAHI' AND [ItemType] = N'Prepared';"));
                Assert.Equal(1, await ScalarAsync<int>(conn, $"SELECT COUNT(1) FROM [Catalog].[ProductVariants] WHERE [Id] = '{variantId}' AND [Sku] = N'SKU-KARAHI-FULL' AND [IsAvailable] = 1;"));
                Assert.Equal(1, await ScalarAsync<int>(conn, $"SELECT COUNT(1) FROM [Inventory].[StockTransfers] WHERE [Id] = '{stockTransferId}' AND [SourceWarehouseId] = '{wh01Id}' AND [DestinationWarehouseId] = '{wh02Id}';"));
                Assert.Equal(2, await ScalarAsync<int>(conn, $"SELECT COUNT(1) FROM [Inventory].[InventoryTransactions] WHERE [ReferenceId] = '{stockTransferId}';"));
                Assert.Equal(1, await ScalarAsync<int>(conn, $"SELECT COUNT(1) FROM [Restaurant].[OutboxMessages] WHERE [Id] = '{outboxMsgId}' AND [IdempotencyKey] = N'idem-prior-1';"));
                Assert.Equal(3, await ScalarAsync<int>(conn, "SELECT COUNT(1) FROM [Restaurant].[PaymentMethods] WHERE [Name] IN (N'Cash', N'Card', N'On Account') AND [Status] = N'Active';"));

                // Verify QuickBooksSyncMaps is physically present and writable post-upgrade
                await using var qbInsertCmd = conn.CreateCommand();
                qbInsertCmd.CommandText = $"""
                    INSERT INTO [Restaurant].[QuickBooksSyncMaps] ([Id], [LocalEntityId], [EntityType], [QuickBooksTxnId], [QuickBooksDocNumber], [Status], [Amount], [Currency], [RetryCount], [CreatedAtUtc], [SyncedAtUtc])
                    VALUES ('{Guid.NewGuid()}', '{Guid.NewGuid()}', N'Invoice', N'QB-TXN-UPG-1', N'ORD-PRIOR-001', N'Synchronized', 1500.0000, N'PKR', 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());
                    """;
                await qbInsertCmd.ExecuteNonQueryAsync();
                Assert.Equal(1, await ScalarAsync<int>(conn, "SELECT COUNT(1) FROM [Restaurant].[QuickBooksSyncMaps];"));
            }

            // 6. Execute repeat provisioning on the already-upgraded database and verify idempotency (no duplicate records or objects)
            var (recreated, recreateMsg) = await provisioningService.CreateDatabaseAsync(settings);
            Assert.True(recreated, recreateMsg);

            var (remigrated, remigrateMsg) = await provisioningService.ApplyMigrationsAsync(connectionString);
            Assert.True(remigrated, remigrateMsg);

            var repeatValidation = await validator.ValidateCompatibilityAsync(connectionString);
            Assert.Equal(SchemaCompatibilityStatus.Compatible, repeatValidation.Status);
            Assert.True(repeatValidation.IsCompatible, repeatValidation.Message);

            await using (var conn = new Microsoft.Data.SqlClient.SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                static async Task<int> CountAsync(Microsoft.Data.SqlClient.SqlConnection c, string sql)
                {
                    await using var cmd = c.CreateCommand();
                    cmd.CommandText = sql;
                    return Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                Assert.Equal(2, await CountAsync(conn, "SELECT COUNT(1) FROM [MasterData].[Warehouses];"));
                Assert.Equal(1, await CountAsync(conn, "SELECT COUNT(1) FROM [Catalog].[ProductCategories];"));
                Assert.Equal(1, await CountAsync(conn, "SELECT COUNT(1) FROM [Catalog].[Products];"));
                Assert.Equal(1, await CountAsync(conn, "SELECT COUNT(1) FROM [Catalog].[ProductVariants];"));
                Assert.Equal(2, await CountAsync(conn, "SELECT COUNT(1) FROM [Inventory].[InventoryTransactions];"));
                Assert.Equal(3, await CountAsync(conn, "SELECT COUNT(1) FROM [Restaurant].[PaymentMethods];"));
                Assert.Equal(1, await CountAsync(conn, "SELECT COUNT(1) FROM [Restaurant].[QuickBooksSyncMaps];"));
            }
        }
        finally
        {
            await DropDisposableDatabaseAsync(dbName);
        }
    }

    private static async Task DropDisposableDatabaseAsync(string dbName)
    {
        Clovent.Desktop.Tests.TestSupport.DatabaseIsolationGuard.EnsureIsolated($"Server=.;Database={dbName};Integrated Security=True;TrustServerCertificate=True;");
        Microsoft.Data.SqlClient.SqlConnection.ClearAllPools();

        await using var masterConn = new Microsoft.Data.SqlClient.SqlConnection("Server=.;Database=master;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=15;");
        await masterConn.OpenAsync();
        await using var dropCmd = masterConn.CreateCommand();
        var escaped = dbName.Replace("]", "]]");
        dropCmd.CommandText = $"""
            IF EXISTS (SELECT 1 FROM sys.databases WHERE name = N'{escaped}')
            BEGIN
                ALTER DATABASE [{escaped}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{escaped}];
            END
            """;
        await dropCmd.ExecuteNonQueryAsync();
    }
}
