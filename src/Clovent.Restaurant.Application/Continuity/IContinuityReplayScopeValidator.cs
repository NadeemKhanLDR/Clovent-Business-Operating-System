using Clovent.MasterData.Warehouses;
using Clovent.Platform.Execution;
using Clovent.Restaurant.Application.Security;

namespace Clovent.Restaurant.Application.Continuity;

/// <summary>Authorizes signed replay targets against the trusted caller and persisted hierarchy.</summary>
public interface IContinuityReplayScopeValidator
{
    /// <summary>Rejects a foreign warehouse or a journal branch inconsistent with that warehouse.</summary>
    Task ValidateAsync(Guid warehouseId, Guid branchId, CancellationToken cancellationToken);
}

/// <summary>A journal's IDs are targets, never authority to establish execution scope.</summary>
public sealed class ContinuityReplayScopeValidator(IOrderScopeValidator scope, IWarehouseRepository warehouses)
    : IContinuityReplayScopeValidator
{
    /// <inheritdoc/>
    public async Task ValidateAsync(Guid warehouseId, Guid branchId, CancellationToken cancellationToken)
    {
        await scope.ValidateWarehouseAccessAsync(warehouseId, cancellationToken);
        var warehouse = await warehouses.GetByIdAsync(new WarehouseId(warehouseId), cancellationToken);
        if (warehouse is null || branchId == Guid.Empty || warehouse.BranchId.Value != branchId)
            throw new ScopeMismatchException("Replay branch does not match the persisted warehouse.");
    }
}
