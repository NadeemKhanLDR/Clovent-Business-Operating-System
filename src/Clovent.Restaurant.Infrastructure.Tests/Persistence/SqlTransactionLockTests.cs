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