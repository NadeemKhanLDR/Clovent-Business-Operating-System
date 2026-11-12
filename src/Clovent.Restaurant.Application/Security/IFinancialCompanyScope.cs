using Clovent.Identity.Branches;
using Clovent.MasterData.Warehouses;
using Clovent.Platform.Execution;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.Shifts;

namespace Clovent.Restaurant.Application.Security;

/// <summary>Runtime financial targets are authorized using persisted owners, never request-supplied authority.</summary>
public interface IFinancialCompanyScope
{
    /// <summary>Authorizes an order and returns its persisted branch.</summary>
    Task<Guid> AuthorizeOrderAsync(Guid orderId, Guid? claimedBranch, CancellationToken cancellationToken,
        Guid? shiftId = null, Guid? organizationId = null);
    /// <summary>Authorizes a customer and optional collection shift.</summary>
    Task AuthorizeCollectionAsync(Guid customerId, Guid? shiftId, CancellationToken cancellationToken);
    /// <summary>Authorizes a report warehouse.</summary>
    Task AuthorizeWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken);
}

/// <summary>Uses the authenticated ambient scope and repositories resolving persisted hierarchy.</summary>
public sealed class FinancialCompanyScope(IExecutionContextAccessor context, IOrderScopeValidator validator,
    IOrderRepository orders, ICustomerRepository customers, IWarehouseRepository warehouses,
    IBranchRepository branches, IShiftRepository shifts) : IFinancialCompanyScope
{
    /// <inheritdoc/>
    public async Task<Guid> AuthorizeOrderAsync(Guid orderId, Guid? claimedBranch, CancellationToken ct,
        Guid? shiftId = null, Guid? organizationId = null)
    {
        var company = RequiredCompany();
        if (organizationId.HasValue && organizationId != context.Current!.OrganizationId)
            throw new ScopeMismatchException("Claimed organization does not match trusted scope.");
        var order = await orders.GetByIdAsync(new OrderId(orderId), ct)
            ?? throw new NotFoundException(nameof(Order), orderId);
        await validator.ValidateWarehouseAccessAsync(order.WarehouseId.Value, ct);
        if (order.CustomerId is { } customerId)
        {
            var customer = await customers.GetByIdAsync(customerId, ct);
            if (customer?.CompanyId != company)
                throw new ScopeMismatchException("Order customer is unmapped or outside the active company.");
        }
        var warehouse = await warehouses.GetByIdAsync(order.WarehouseId, ct)
            ?? throw new ScopeMismatchException("Financial warehouse cannot be resolved.");
        if (claimedBranch.HasValue && claimedBranch != warehouse.BranchId.Value)
            throw new ScopeMismatchException("Claimed branch does not match the persisted order.");
        if (shiftId is { } rawShift)
        {
            var shift = await shifts.GetByIdAsync(new ShiftId(rawShift), ct);
            var branch = shift is null ? null : await branches.GetByIdAsync(shift.BranchId, ct);
            if (branch?.CompanyId.Value != company || shift?.BranchId != warehouse.BranchId)
                throw new ScopeMismatchException("Financial shift does not match the authorized company/branch.");
        }
        return warehouse.BranchId.Value;
    }

    /// <inheritdoc/>
    public async Task AuthorizeCollectionAsync(Guid customerId, Guid? shiftId, CancellationToken ct)
    {
        var company = RequiredCompany();
        var customer = await customers.GetByIdAsync(new CustomerId(customerId), ct);
        if (customer?.CompanyId != company)
            throw new ScopeMismatchException("Collection customer is outside the active company.");
        if (shiftId is not { } rawShift) return;
        var shift = await shifts.GetByIdAsync(new ShiftId(rawShift), ct)
            ?? throw new ScopeMismatchException("Collection shift cannot be resolved.");
        var branch = await branches.GetByIdAsync(shift.BranchId, ct);
        if (branch?.CompanyId.Value != company ||
            (context.Current?.BranchId is { } activeBranch && activeBranch != shift.BranchId.Value))
            throw new ScopeMismatchException("Collection shift is outside the active scope.");
    }

    /// <inheritdoc/>
    public Task AuthorizeWarehouseAsync(Guid warehouseId, CancellationToken ct)
    {
        RequiredCompany();
        return validator.ValidateWarehouseAccessAsync(warehouseId, ct);
    }

    private Guid RequiredCompany()
    {
        if (context.Current?.CompanyId is not { } company || company == Guid.Empty ||
            context.Current.OrganizationId is not { } organization || organization == Guid.Empty)
            throw new ScopeUnresolvedException("Trusted company and organization scope are required.");
        return company;
    }
}
