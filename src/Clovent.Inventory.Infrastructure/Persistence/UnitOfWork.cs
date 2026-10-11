using Clovent.Inventory.Application;
using Microsoft.EntityFrameworkCore;

namespace Clovent.Inventory.Infrastructure.Persistence;

/// <summary>EF Core implementation of <see cref="IUnitOfWork"/>, wrapping <see cref="InventoryDbContext"/>'s <c>SaveChangesAsync</c>. Mirrors <c>Clovent.Catalog.Infrastructure.Persistence.UnitOfWork</c>.</summary>
public sealed class UnitOfWork(InventoryDbContext dbContext) : IUnitOfWork
{
    /// <inheritdoc/>
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<TResult> ExecuteStockMovementAtomicallyAsync<TResult>(
        Guid warehouseId,
        Guid productVariantId,
        Func<CancellationToken, Task<TResult>> operation,
        Func<CancellationToken, Task<TResult>> onDuplicateReferenceRecovery,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(onDuplicateReferenceRecovery);

        if (dbContext.Database.CurrentTransaction is not null)
        {
            var nestedResult = await operation(cancellationToken).ConfigureAwait(false);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return nestedResult;
        }

        await using var dbTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (dbContext.Database.IsSqlServer())
            {
                var lockResource = $"CBOS_INV_STOCK_{warehouseId:N}_{productVariantId:N}";
                await dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $"DECLARE @res int; EXEC @res = sp_getapplock @Resource = {lockResource}, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 15000; IF @res < 0 THROW 50001, 'Failed to acquire inventory stock lock.', 1;",
                    cancellationToken).ConfigureAwait(false);
            }

            // Clear any stale tracked entities so reads inside the locked transaction reflect the latest committed database state
            dbContext.ChangeTracker.Clear();

            var result = await operation(cancellationToken).ConfigureAwait(false);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await dbTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch (DbUpdateException ex) when (IsDuplicateReferenceConstraintViolation(ex))
        {
            await dbTransaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            dbContext.ChangeTracker.Clear();
            return await onDuplicateReferenceRecovery(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await dbTransaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            dbContext.ChangeTracker.Clear();
            throw;
        }
    }

    private static bool IsDuplicateReferenceConstraintViolation(DbUpdateException ex)
    {
        var msg = ex.InnerException?.Message ?? ex.Message;
        return msg.Contains("IX_InventoryTransactions_Reference_Deduplication", StringComparison.OrdinalIgnoreCase) ||
               msg.Contains("Cannot insert duplicate key", StringComparison.OrdinalIgnoreCase) ||
               msg.Contains("UNIQUE constraint failed: InventoryTransactions.ReferenceType, InventoryTransactions.ReferenceId, InventoryTransactions.WarehouseId, InventoryTransactions.ProductVariantId", StringComparison.OrdinalIgnoreCase);
    }
}
