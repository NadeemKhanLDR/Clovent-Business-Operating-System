using Clovent.Catalog.Variants;
using Clovent.Inventory.Application;
using Clovent.Inventory.Transactions;
using Clovent.Inventory.WarehouseStocks;
using Clovent.MasterData.Warehouses;
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

            // Detach only Unchanged entities so reads inside the locked transaction reflect fresh database state
            // without discarding unrelated pending changes in the shared unit of work.
            DetachUnchangedEntities(dbContext);

            var result = await operation(cancellationToken).ConfigureAwait(false);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await dbTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch (DbUpdateException ex) when (IsDuplicateReferenceConstraintViolation(ex))
        {
            await dbTransaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            DetachStaleOrRolledBackEntities(dbContext, warehouseId, productVariantId);
            return await onDuplicateReferenceRecovery(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await dbTransaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            DetachStaleOrRolledBackEntities(dbContext, warehouseId, productVariantId);
            throw;
        }
    }

    private static void DetachUnchangedEntities(DbContext context)
    {
        foreach (var entry in context.ChangeTracker.Entries().Where(e => e.State == EntityState.Unchanged).ToList())
        {
            entry.State = EntityState.Detached;
        }
    }

    private static void DetaleOrRolledBackEntities(DbContext context, Guid warehouseId, Guid productVariantId)
    {
        DetachStaleOrRolledBackEntities(context, warehouseId, productVariantId);
    }

    private static void DetachStaleOrRolledBackEntities(DbContext context, Guid warehouseId, Guid productVariantId)
    {
        var wid = new WarehouseId(warehouseId);
        var vid = new ProductVariantId(productVariantId);

        foreach (var entry in context.ChangeTracker.Entries().ToList())
        {
            if (entry.State == EntityState.Unchanged)
            {
                entry.State = EntityState.Detached;
            }
            else if (entry.Entity is WarehouseStock ws && ws.WarehouseId == wid && ws.ProductVariantId == vid)
            {
                entry.State = EntityState.Detached;
            }
            else if (entry.Entity is InventoryTransaction it && it.WarehouseId == wid && it.ProductVariantId == vid)
            {
                entry.State = EntityState.Detached;
            }
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
