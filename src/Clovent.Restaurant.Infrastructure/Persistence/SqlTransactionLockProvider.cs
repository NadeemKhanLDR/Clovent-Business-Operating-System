using System.Data;
using Clovent.Restaurant.Application.Concurrency;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Clovent.Restaurant.Infrastructure.Persistence;

/// <summary>Database-scoped SQL session locks spanning handler execution and SaveChanges.</summary>
public sealed class SqlTransactionLockProvider(RestaurantDbContext db, ILogger<SqlTransactionLockProvider>? logger = null) : ITransactionLockProvider
{
    /// <inheritdoc/>
    public async Task<T> ExecuteAsync<T>(IReadOnlyList<TransactionLockKey> keys, CancellationToken cancellationToken, Func<Task<T>> action)
    {
        if (!db.Database.IsSqlServer() || keys.Count == 0) return await action().ConfigureAwait(false);
        var connection = db.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;
        var acquired = new List<string>();
        try
        {
            if (openedHere) await db.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            foreach (var key in keys
                .Where(k => k.EntityId != Guid.Empty)
                .GroupBy(k => k.Resource)
                .Select(g => new TransactionLockKey(
                    g.Key.Scope,
                    g.Key.EntityId,
                    g.Any(k => k.AccessMode == TransactionLockAccessMode.Exclusive)
                        ? TransactionLockAccessMode.Exclusive
                        : TransactionLockAccessMode.Shared))
                .OrderBy(k => k))
            {
                var resource = $"CBOS:financial:{(int)key.Scope}:{key.EntityId:N}";
                var lockMode = key.AccessMode == TransactionLockAccessMode.Shared ? "Shared" : "Exclusive";
                await using var command = connection.CreateCommand();
                command.CommandText = "DECLARE @result int; EXEC @result = sys.sp_getapplock @Resource=@resource, @LockMode=@lockMode, @LockOwner=N'Session', @LockTimeout=10000; SELECT @result;";
                command.CommandTimeout = 15;
                var parameter = command.CreateParameter(); parameter.ParameterName = "@resource"; parameter.Value = resource; command.Parameters.Add(parameter);
                var modeParameter = command.CreateParameter(); modeParameter.ParameterName = "@lockMode"; modeParameter.Value = lockMode; command.Parameters.Add(modeParameter);
                // Track even an unacknowledged acquisition so cancellation cannot
                // leave a possibly granted session lock in the pool.
                acquired.Add(resource);
                int result;
                try { result = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)); }
                catch (SqlException error) when (cancellationToken.IsCancellationRequested)
                { throw new OperationCanceledException("Canceled while waiting for financial database coordination.", error, cancellationToken); }
                cancellationToken.ThrowIfCancellationRequested();
                if (result < 0) throw new TimeoutException($"Financial operation could not acquire its database lock (result {result}). No command was executed; refresh and retry the same attempt.");

            }
            return await action().ConfigureAwait(false);
        }
        finally
        {
            foreach (var resource in acquired.AsEnumerable().Reverse())
            {
                try
                {
                    await using var command = connection.CreateCommand();
                    command.CommandText = "DECLARE @result int; EXEC @result = sys.sp_releaseapplock @Resource=@resource, @LockOwner=N'Session'; SELECT @result;";
                    command.CommandTimeout = 5;
                    var parameter = command.CreateParameter(); parameter.ParameterName = "@resource"; parameter.Value = resource; command.Parameters.Add(parameter);
                    if (Convert.ToInt32(await command.ExecuteScalarAsync(CancellationToken.None).ConfigureAwait(false)) < 0)
                        throw new InvalidOperationException("Database session lock release failed.");
                }
                catch (Exception error) when (error is not OutOfMemoryException)
                {
                    // Release failure after a commit must not turn saved money
                    // into a reported transaction failure. Retire this connection.
                    logger?.LogWarning(error, "Could not release financial database session lock");
                    if (connection is SqlConnection sql) SqlConnection.ClearPool(sql);
                    try { await connection.CloseAsync().ConfigureAwait(false); }
                    catch (Exception closeError) when (closeError is not OutOfMemoryException)
                    { logger?.LogWarning(closeError, "Could not close retired financial database connection"); }
                    break;
                }
            }
            if (openedHere)
            {
                try { await db.Database.CloseConnectionAsync().ConfigureAwait(false); }
                catch (Exception closeError) when (closeError is not OutOfMemoryException)
                {
                    if (connection is SqlConnection sql) SqlConnection.ClearPool(sql);
                    logger?.LogWarning(closeError, "Could not close financial database connection after operation");
                }
            }
        }
    }
}
