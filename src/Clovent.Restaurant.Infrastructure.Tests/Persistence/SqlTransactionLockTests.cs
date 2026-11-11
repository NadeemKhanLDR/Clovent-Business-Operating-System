using System.Data;
using Clovent.Restaurant.Application.Concurrency;
using Clovent.Restaurant.Infrastructure.Persistence;
using Clovent.Restaurant.Infrastructure.Tests.TestSupport;
using Clovent.Restaurant.PaymentMethods;
using Clovent.Restaurant.PaymentMethods.ValueObjects;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.Persistence;

public sealed class SqlTransactionLockTests
{
    [Fact]
    public async Task FailureAndCanceledWait_ReleaseSessionLocksWithoutExecutingWaitingBody()
    {
        var options = new DbContextOptionsBuilder<RestaurantDbContext>()
            .UseSqlServer(SqlTestDatabaseGuard.ConnectionString($"CBOS_Recovery_{Guid.NewGuid():N}")).Options;
        await using var first = new RestaurantDbContext(options);
        await using var second = new RestaurantDbContext(options);
        var key = TransactionLockKey.ForOrder(Guid.NewGuid());
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<int>? holder = null;
        try
        {
            await first.Database.EnsureCreatedAsync();
            await SqlTestDatabaseGuard.CreatedAsync(first);
            var firstLocks = new SqlTransactionLockProvider(first);
            var secondLocks = new SqlTransactionLockProvider(second);
            holder = firstLocks.ExecuteAsync<int>([key], CancellationToken.None, async () =>
            {
                entered.SetResult();
                await release.Task.WaitAsync(TimeSpan.FromSeconds(15));
                throw new InvalidOperationException("injected handler failure");
            });
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var executed = false;
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => secondLocks.ExecuteAsync([key], cancellation.Token,
                () => { executed = true; return Task.FromResult(1); }));
            Assert.False(executed);
            release.TrySetResult();
            await Assert.ThrowsAsync<InvalidOperationException>(() => holder);
            Assert.Equal(42, await secondLocks.ExecuteAsync([key], CancellationToken.None, () => Task.FromResult(42)));
        }
        finally
        {
            release.TrySetResult();
            if (holder is not null) { try { await holder; } catch (InvalidOperationException) { } }
            await SqlTestDatabaseGuard.DeleteAndVerifyAsync(first);
        }
    }

    [Fact]
    public async Task LeastPrivilegedDatabaseUser_ExecutesAppLockContentionAndFinancialDml_WithoutOwnerOrControlPermissions()
    {
        var dbName = $"CBOS_Recovery_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<RestaurantDbContext>()
            .UseSqlServer(SqlTestDatabaseGuard.ConnectionString(dbName)).Options;

        await using var adminDb = new RestaurantDbContext(options);
        await using var constrainedFirst = new RestaurantDbContext(options);
        await using var constrainedSecond = new RestaurantDbContext(options);
        var key = TransactionLockKey.ForOrder(Guid.NewGuid());
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task? holder = null;

        try
        {
            await adminDb.Database.EnsureCreatedAsync();
            await SqlTestDatabaseGuard.CreatedAsync(adminDb);

            // Create a constrained database-only user with ONLY minimal Restaurant schema DML permissions
            // (no db_owner, no CONTROL, no server-level login/permissions).
            await adminDb.Database.ExecuteSqlRawAsync("""
                CREATE USER [cbos_lock_tester] WITHOUT LOGIN;
                GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[Restaurant] TO [cbos_lock_tester];
                """);

            await constrainedFirst.Database.OpenConnectionAsync();
            await constrainedSecond.Database.OpenConnectionAsync();
            await ImpersonateAndAssertLeastPrivilegeAsync(constrainedFirst);
            await ImpersonateAndAssertLeastPrivilegeAsync(constrainedSecond);

            var firstLocks = new SqlTransactionLockProvider(constrainedFirst);
            var secondLocks = new SqlTransactionLockProvider(constrainedSecond);

            holder = firstLocks.ExecuteAsync<int>([key], CancellationToken.None, async () =>
            {
                entered.SetResult();
                await release.Task.WaitAsync(TimeSpan.FromSeconds(15));
                return 1;
            });

            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));

            // Second constrained session must block on the same CBOS:financial SQL app lock and honor cancellation
            var waiterEntered = false;
            using var cancelSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                secondLocks.ExecuteAsync<int>([key], cancelSource.Token, () =>
                {
                    waiterEntered = true;
                    return Task.FromResult(99);
                }));
            Assert.False(waiterEntered);

            // Release first lock cleanly; second constrained session must acquire lock and execute EF DML
            release.TrySetResult();
            await holder;

            var insertedName = $"Cash-{Guid.NewGuid():N}"[..24];
            var persistedCount = await secondLocks.ExecuteAsync<int>([key], CancellationToken.None, async () =>
            {
                constrainedSecond.PaymentMethods.Add(PaymentMethod.Create(PaymentMethodName.Create(insertedName)));
                return await constrainedSecond.SaveChangesAsync();
            });

            Assert.Equal(1, persistedCount);
        }
        finally
        {
            release.TrySetResult();
            if (holder is not null) { try { await holder; } catch { } }

            await RevertAndClearPoolAsync(constrainedFirst);
            await RevertAndClearPoolAsync(constrainedSecond);
            await SqlTestDatabaseGuard.DeleteAndVerifyAsync(adminDb);
        }
    }

    [Fact]
    public async Task SharedVsExclusiveLocks_AllowConcurrentSharedReadersAndSerializeExclusiveWriterOnLocalDb()
    {
        var dbName = $"CBOS_Recovery_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<RestaurantDbContext>()
            .UseSqlServer(SqlTestDatabaseGuard.ConnectionString(dbName)).Options;

        await using var db1 = new RestaurantDbContext(options);
        await using var db2 = new RestaurantDbContext(options);
        await using var db3 = new RestaurantDbContext(options);
        await using var db4 = new RestaurantDbContext(options);

        var branchId = Guid.NewGuid();
        var sharedKey = TransactionLockKey.ForBusinessDay(branchId, TransactionLockAccessMode.Shared);
        var exclusiveKey = TransactionLockKey.ForBusinessDay(branchId, TransactionLockAccessMode.Exclusive);

        var shared1Entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var shared2Entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseShared = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var exclusiveEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseExclusive = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Task? sharedTask1 = null;
        Task? sharedTask2 = null;
        Task? exclusiveTask = null;
        Task? lateSharedTask = null;

        try
        {
            await db1.Database.EnsureCreatedAsync();
            await SqlTestDatabaseGuard.CreatedAsync(db1);

            var l1 = new SqlTransactionLockProvider(db1);
            var l2 = new SqlTransactionLockProvider(db2);
            var l3 = new SqlTransactionLockProvider(db3);
            var l4 = new SqlTransactionLockProvider(db4);

            sharedTask1 = l1.ExecuteAsync<int>([sharedKey], CancellationToken.None, async () =>
            {
                shared1Entered.SetResult();
                await releaseShared.Task.WaitAsync(TimeSpan.FromSeconds(15));
                return 1;
            });

            sharedTask2 = l2.ExecuteAsync<int>([sharedKey], CancellationToken.None, async () =>
            {
                shared2Entered.SetResult();
                await releaseShared.Task.WaitAsync(TimeSpan.FromSeconds(15));
                return 2;
            });

            // Both Shared holders enter concurrently across separate SQL sessions
            await Task.WhenAll(shared1Entered.Task, shared2Entered.Task).WaitAsync(TimeSpan.FromSeconds(10));

            exclusiveTask = l3.ExecuteAsync<int>([exclusiveKey], CancellationToken.None, async () =>
            {
                exclusiveEntered.SetResult();
                await releaseExclusive.Task.WaitAsync(TimeSpan.FromSeconds(15));
                return 3;
            });

            await Task.Delay(150);
            Assert.False(exclusiveEntered.Task.IsCompleted);

            releaseShared.TrySetResult();
            await Task.WhenAll(sharedTask1, sharedTask2).WaitAsync(TimeSpan.FromSeconds(10));
            await exclusiveEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));

            var lateSharedEntered = false;
            lateSharedTask = l4.ExecuteAsync<int>([sharedKey], CancellationToken.None, () =>
            {
                lateSharedEntered = true;
                return Task.FromResult(4);
            });

            await Task.Delay(150);
            Assert.False(lateSharedEntered);

            releaseExclusive.TrySetResult();
            await Task.WhenAll(exclusiveTask, lateSharedTask).WaitAsync(TimeSpan.FromSeconds(10));
            Assert.True(lateSharedEntered);
        }
        finally
        {
            releaseShared.TrySetResult();
            releaseExclusive.TrySetResult();
            foreach (var t in new[] { sharedTask1, sharedTask2, exclusiveTask, lateSharedTask })
            {
                if (t is not null) { try { await t; } catch { } }
            }
            await SqlTestDatabaseGuard.DeleteAndVerifyAsync(db1);
        }
    }

    private static async Task ImpersonateAndAssertLeastPrivilegeAsync(RestaurantDbContext db)
    {
        var conn = db.Database.GetDbConnection();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            EXECUTE AS USER = 'cbos_lock_tester';
            SELECT
                USER_NAME() AS CurrentUser,
                ISNULL(IS_ROLEMEMBER('db_owner'), 0) AS IsDbOwner,
                ISNULL(IS_SRVROLEMEMBER('sysadmin'), 0) AS IsSysAdmin,
                HAS_PERMS_BY_NAME(NULL, 'DATABASE', 'CONTROL') AS HasControl;
            """;

        await using var reader = await cmd.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal("cbos_lock_tester", reader.GetString(0));
        Assert.Equal(0, Convert.ToInt32(reader.GetValue(1)));
        Assert.Equal(0, Convert.ToInt32(reader.GetValue(2)));
        Assert.Equal(0, Convert.ToInt32(reader.GetValue(3)));
    }

    private static async Task RevertAndClearPoolAsync(RestaurantDbContext db)
    {
        try
        {
            var conn = db.Database.GetDbConnection();
            if (conn.State == ConnectionState.Open)
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "IF USER_NAME() = 'cbos_lock_tester' REVERT;";
                await cmd.ExecuteNonQueryAsync();
            }

            if (conn is SqlConnection sqlConn)
            {
                SqlConnection.ClearPool(sqlConn);
            }
        }
        catch
        {
            // Best-effort cleanup before disposable database drop
        }
    }
}