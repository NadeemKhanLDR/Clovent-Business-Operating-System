using Clovent.Restaurant.Orders;

namespace Clovent.Restaurant.Application.Security;

/// <summary>
/// Authoritatively validates tenancy ownership of orders and warehouses against the caller's active execution context.
/// Ensures callers cannot read, mutate, or report on entities outside their authorized branch or company scope.
/// </summary>
public interface IOrderScopeValidator
{
    /// <summary>
    /// Validates that an order's owning branch (resolved through its persisted warehouse) matches the caller's authorized branch scope.
    /// Throws <see cref="Clovent.Platform.Execution.ScopeUnresolvedException"/> if scope is missing, or
    /// <see cref="Clovent.Platform.Execution.ScopeMismatchException"/> if branch mismatch occurs.
    /// </summary>
    Task ValidateOrderAccessAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that a warehouse's owning company (resolved through its persisted branch) matches the caller's authorized company scope.
    /// Throws <see cref="Clovent.Platform.Execution.ScopeUnresolvedException"/> if scope is missing,
    /// <see cref="NotFoundException"/> if warehouse is not found, or
    /// <see cref="Clovent.Platform.Execution.ScopeMismatchException"/> if company mismatch occurs.
    /// </summary>
    Task ValidateWarehouseCompanyAccessAsync(Guid warehouseId, CancellationToken cancellationToken = default);
}
