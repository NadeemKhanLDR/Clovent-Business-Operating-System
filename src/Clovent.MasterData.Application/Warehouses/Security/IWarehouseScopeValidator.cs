using Clovent.Identity.Branches;
using Clovent.MasterData.Warehouses;

namespace Clovent.MasterData.Application.Warehouses.Security;

/// <summary>
/// Authoritatively validates and resolves execution scope boundaries for warehouses and branches.
/// Ensures that multi-tenant boundaries cannot be bypassed by direct entity requests or query filters.
/// </summary>
public interface IWarehouseScopeValidator
{
    /// <summary>
    /// Validates that the specified warehouse belongs to an authorized branch under the caller's active company scope.
    /// Returns the verified <see cref="Warehouse"/> domain aggregate.
    /// </summary>
    Task<Warehouse> ValidateAndGetWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that the specified branch belongs to the caller's active company scope.
    /// Returns the verified <see cref="Branch"/> domain aggregate.
    /// </summary>
    Task<Branch> ValidateAndGetBranchAsync(Guid branchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all branch IDs authorized under the caller's active company scope.
    /// </summary>
    Task<IReadOnlySet<BranchId>> GetAuthorizedBranchIdsAsync(CancellationToken cancellationToken = default);
}
