using Clovent.Desktop.Commissioning.Database;
using Clovent.Desktop.Configuration;
using Clovent.Desktop.Tests.TestSupport;
using Microsoft.Data.SqlClient;
using Xunit;

namespace Clovent.Desktop.Tests.Commissioning;

public sealed class DatabaseBackupAndRestoreTests
{
    [Fact]
    public void BackupHistoryStore_RecordAndLoad_PersistsSortedAndReturnsLastSuccessful()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"cbos-backup-history-{Guid.NewGuid():N}.json");
        try
        {
            var store = new BackupHistoryStore(tempFile);
            Assert.Empty(store.LoadAll());
            Assert.Null(store.GetLastSuccessfulBackup());

            var older = new BackupHistoryEntry(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow.AddHours(-2),
                "Backup",
                "Clovent_Test",
                @"C:\Backups\old.bak",
                1024,
                Verified: true,
                Success: true,
                "Old backup",
                "tester");

            var failedNewer = new BackupHistoryEntry(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow.AddHours(-1),
                "Backup",
                "Clovent_Test",
                @"C:\Backups\failed.bak",
                null,
                Verified: false,
                Success: false,
                "Failed",
                "tester");

            store.Record(older);
            store.Record(failedNewer);

            var all = store.LoadAll();
            Assert.Equal(2, all.Count);
            Assert.Equal(failedNewer.Id, all[0].Id);

            var lastOk = store.GetLastSuccessfulBackup();
            Assert.NotNull(lastOk);
            Assert.Equal(older.Id, lastOk.Id);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void BackupConfigurationStore_SaveAndLoad_NormalizesInvalidValues()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"cbos-backup-cfg-{Guid.NewGuid():N}.json");
        try
        {
            var store = new BackupConfigurationStore(tempFile);
            store.Save(new BackupConfiguration
            {
                BackupDirectory = @"D:\CBOS_Backups",
                AutoBackupBeforeUpgrade = true,
                ScheduledBackupEnabled = true,
                ScheduledBackupIntervalHours = 0,
                RetentionCount = -5
            });

            var loaded = store.Load();
            Assert.Equal(@"D:\CBOS_Backups", loaded.BackupDirectory);
            Assert.True(loaded.AutoBackupBeforeUpgrade);
            Assert.True(loaded.ScheduledBackupEnabled);
            Assert.Equal(1, loaded.ScheduledBackupIntervalHours);
            Assert.Equal(0, loaded.RetentionCount);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task VerifyAndRestore_InvalidArguments_FailSafely()
    {
        var service = new DatabaseBackupService();

        var verifyEmptyCs = await service.VerifyBackupAsync(string.Empty, @"C:\fake.bak");
        Assert.False(verifyEmptyCs.Success);

        var verifyEmptyPath = await service.VerifyBackupAsync("Server=.;Database=master;Trusted_Connection=True;", string.Empty);
        Assert.False(verifyEmptyPath.Success);

        var restoreEmptyCs = await service.RestoreDatabaseAsync(string.Empty, @"C:\fake.bak");
        Assert.False(restoreEmptyCs.Success);

        var restoreEmptyPath = await service.RestoreDatabaseAsync("Server=.;Database=master;Trusted_Connection=True;", string.Empty);
        Assert.False(restoreEmptyPath.Success);
    }

    [Fact]
    public async Task CreateVerifyAndRestoreIntoIsolatedDatabase_PreservesSentinelData()
    {
        var suffix = Guid.NewGuid().ToString("N")[..12];
        var sourceDb = $"CBOS_Disposable_BackupSrc_{suffix}";
        var targetDb = $"CBOS_Disposable_RestoreIso_{suffix}";

        var sourceSettings = new DatabaseConnectionSettings
        {
            Server = ".",
            Database = sourceDb,
            UseWindowsAuthentication = true,
            TrustServerCertificate = true,
            ConnectionTimeout = 15
        };

        var sourceCs = sourceSettings.BuildConnectionString();
        DatabaseIsolationGuard.EnsureIsolated(sourceCs);

        var masterCs = new DatabaseConnectionSettings
        {
            Server = ".",
            Database = "master",
            UseWindowsAuthentication = true,
            TrustServerCertificate = true,
            ConnectionTimeout = 15
        }.BuildConnectionString();

        var tempHistoryPath = Path.Combine(Path.GetTempPath(), $"cbos-hist-{suffix}.json");

        string? createdBackupPath = null;

        try
        {
            var provisioning = new DatabaseProvisioningService();
            var (created, createMsg) = await provisioning.CreateDatabaseAsync(sourceSettings);
            Assert.True(created, createMsg);

            const string sentinelValue = "CBOS-RESTORE-VERIFIED-2026";
            await using (var srcConn = new SqlConnection(sourceCs))
            {
                await srcConn.OpenAsync();
                await using var cmd = srcConn.CreateCommand();
                cmd.CommandText = $"""
                    CREATE TABLE dbo.BackupVerificationSentinel (Id INT NOT NULL PRIMARY KEY, Marker NVARCHAR(100) NOT NULL);
                    INSERT INTO dbo.BackupVerificationSentinel (Id, Marker) VALUES (1, N'{sentinelValue}');
                    """;
                await cmd.ExecuteNonQueryAsync();
            }

            var backupService = new DatabaseBackupService(historyStore: new BackupHistoryStore(tempHistoryPath));

            // 1. Create verified COPY_ONLY backup in SQL Server's InstanceDefaultBackupPath
            var (backupOk, backupPath, backupMsg) = await backupService.CreateBackupAsync(sourceCs, targetDirectory: null);
            Assert.True(backupOk, backupMsg);
            Assert.False(string.IsNullOrWhiteSpace(backupPath));
            createdBackupPath = backupPath;

            // 2. Verify backup header and integrity
            var verifyResult = await backupService.VerifyBackupAsync(sourceCs, backupPath);
            Assert.True(verifyResult.Success, verifyResult.Message);
            Assert.Equal(sourceDb, verifyResult.DatabaseName);
            Assert.True(verifyResult.IsCopyOnly);

            // 3. Restore into isolated target database
            var restoreResult = await backupService.RestoreDatabaseAsync(
                sourceCs,
                backupPath,
                targetDatabaseName: targetDb,
                createPreRestoreSafetyBackup: false);
            Assert.True(restoreResult.Success, restoreResult.Message);
            Assert.Equal(targetDb, restoreResult.TargetDatabase);

            // 4. Verify sentinel row exists in restored isolated database
            var targetCs = new DatabaseConnectionSettings
            {
                Server = ".",
                Database = targetDb,
                UseWindowsAuthentication = true,
                TrustServerCertificate = true,
                ConnectionTimeout = 15
            }.BuildConnectionString();
            DatabaseIsolationGuard.EnsureIsolated(targetCs);

            await using (var dstConn = new SqlConnection(targetCs))
            {
                await dstConn.OpenAsync();
                await using var cmd = dstConn.CreateCommand();
                cmd.CommandText = "SELECT Marker FROM dbo.BackupVerificationSentinel WHERE Id = 1;";
                var actual = await cmd.ExecuteScalarAsync() as string;
                Assert.Equal(sentinelValue, actual);
            }
        }
        finally
        {
            SqlConnection.ClearAllPools();
            await DropDatabaseIfExistsAsync(masterCs, sourceDb);
            await DropDatabaseIfExistsAsync(masterCs, targetDb);
            if (!string.IsNullOrEmpty(createdBackupPath))
            {
                try
                {
                    if (File.Exists(createdBackupPath))
                    {
                        File.Delete(createdBackupPath);
                    }
                }
                catch
                {
                    // Best-effort cleanup if SQL backup folder is restricted
                }
            }
            if (File.Exists(tempHistoryPath))
            {
                File.Delete(tempHistoryPath);
            }
        }
    }

    private static async Task DropDatabaseIfExistsAsync(string masterCs, string dbName)
    {
        try
        {
            await using var conn = new SqlConnection(masterCs);
            await conn.OpenAsync();
            var escaped = dbName.Replace("]", "]]");
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"""
                IF DB_ID(@db) IS NOT NULL
                BEGIN
                    ALTER DATABASE [{escaped}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{escaped}];
                END;
                """;
            cmd.Parameters.AddWithValue("@db", dbName);
            await cmd.ExecuteNonQueryAsync();
        }
        catch
        {
            // Best-effort cleanup
        }
    }
}
