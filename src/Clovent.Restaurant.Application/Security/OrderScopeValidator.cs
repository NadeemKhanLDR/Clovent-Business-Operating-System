using Clovent.Identity.Branches;
using Clovent.MasterData.Warehouses;
using Clovent.Platform.Execution;
using Clovent.Restaurant.Orders;

namespace Clovent.Restaurant.Application.Security;

/// <summary>
/// Default implementation of <see cref="IOrderScopeValidator"/>.
/// Resolves the persisted ownership hierarchy (Order -> Warehouse -> Branch -> Company) and enforces
/// that callers can only access and modify entities within their authorized company and branch boundaries.
/// </summary>
public sealed class OrderScopeValidator(
    IExecutionContextAccessor contextAccessor,
    IWarehouseRepository warehouseRepository,
    IBranchRepository branchRepository) : IOrderScopeValidator
{
    private readonly IExecutionContextAccessor _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
    private readonly IWarehouseRepository _warehouseRepository = warehouseRepository ?? throw new ArgumentNullException(nameof(warehouseRepository));
    private readonly IBranchRepository _branchRepository = branchRepository ?? throw new ArgumentNullException(nameof(branchRepository));

    /// <inheritdoc />
    public async Task ValidateOrderAccessAsync(Order order, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);

        var current = _contextAccessor.Current
            ?? throw new ScopeUnresolvedException("Execution context is not established. Null scope never grants global access.");

        if (!current.BranchId.HasValue || current.BranchId.Value == Guid.Empty)
        {
            throw new ScopeUnresolvedException(
                $"Branch scope is required to access order '{order.Id.Value}', but current branch scope is unresolved.");
        }

        var warehouse = await _warehouseRepository.GetByIdAsync(order.WarehouseId, cancellationToken).ConfigureAwait(false)
            ?? throw new ScopeMismatchException(
                $"Warehouse '{order.WarehouseId.Value}' associated with order '{order.Id.Value}' was not found.");

        if (warehouse.BranchId.Value != current.BranchId.Value)
        {
            throw new ScopeMismatchException(
                $"Access denied: Order '{order.Id.Value}' belongs to branch '{warehouse.BranchId.Value}' (via warehouse '{warehouse.Id.Value}'), " +
                $"but caller scope is restricted to branch '{current.BranchId.Value}'.");
        }
    }

    /// <inheritdoc />
    public async Task ValidateWarehouseCompanyAccessAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        if (warehouseId == Guid.Empty)
        {
            throw new ScopeUnresolvedException("Warehouse identifier cannot be empty.");
        }

        var current = _contextAccessor.Current
            ?? throw new ScopeUnresolvedException("Execution context is not established. Null scope never grants global access.");

        if (!current.CompanyId.HasValue || current.CompanyId.Value == Guid.Empty)
        {
            throw new ScopeUnresolvedException(
                $"Company scope is required to access warehouse '{warehouseId}', but current company scope is unresolved.");
        }

        var warehouse = await _warehouseRepository.GetByIdAsync(new WarehouseId(warehouseId), cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Warehouse), warehouseId);

        var branch = await _branchRepository.GetByIdAsync(warehouse.BranchId, cancellationToken).ConfigureAwait(false)
            ?? throw new ScopeMismatchException(
                $"Owning branch '{warehouse.BranchId.Value}' for warehouse '{warehouseId}' was not found.");

        if (branch.CompanyId.Value != current.CompanyId.Value)
        {
            throw new ScopeMismatchException(
                $"Access denied: Warehouse '{warehouseId}' belongs to company '{branch.CompanyId.Value}' (via branch '{branch.Id.Value}'), " +
                $"but caller scope is restricted to company '{current.CompanyId.Value}'.");
        }
    }

    /// <inheritdoc />
    public async Task ValidateWarehouseAccessAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        if (warehouseId == Guid.Empty)
        {
            throw new ScopeUnresolvedException("Warehouse identifier cannot be empty.");
        }

        var current = _contextAccessor.Current
            ?? throw new ScopeUnresolvedException("Execution context is not established. Null scope never grants global access.");

        if (!current.CompanyId.HasValue || current.CompanyId.Value == Guid.Empty)
        {
            throw new ScopeUnresolvedException(
                $"Company scope is required to access warehouse '{warehouseId}', but current company scope is unresolved.");
        }

        var warehouse = await _warehouseRepository.GetByIdAsync(new WarehouseId(warehouseId), cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Warehouse), warehouseId);

        var branch = await _branchRepository.GetByIdAsync(warehouse.BranchId, cancellationToken).ConfigureAwait(false)
            ?? throw new ScopeMismatchException(
                $"Owning branch '{warehouse.BranchId.Value}' for warehouse '{warehouseId}' was not found.");

        if (branch.CompanyId.Value != current.CompanyId.Value)
        {
            throw new ScopeMismatchException(
                $"Access denied: Warehouse '{warehouseId}' belongs to company '{branch.CompanyId.Value}' (via branch '{branch.Id.Value}'), " +
                $"but caller scope is restricted to company '{current.CompanyId.Value}'.");
        }

        if (current.BranchId.HasValue && current.BranchId.Value != Guid.Empty && warehouse.BranchId.Value != current.BranchId.Value)
        {
            throw new ScopeMismatchException(
                $"Access denied: Warehouse '{warehouseId}' belongs to branch '{warehouse.BranchId.Value}', " +
                $"but caller scope is restricted to branch '{current.BranchId.Value}'.");
        }
    }
}
