using Clovent.Identity.Branches;
using Clovent.Identity.Companies;
using Clovent.MasterData.Warehouses;
using Clovent.Platform.Execution;

namespace Clovent.MasterData.Application.Warehouses.Security;

/// <summary>
/// Default implementation of <see cref="IWarehouseScopeValidator"/>.
/// Resolves the persisted ownership hierarchy (Warehouse -> Branch -> Company) and enforces
/// that callers can only access and modify entities within their authorized company boundaries.
/// Fails closed if ambient or interactive execution context is missing or incomplete.
/// </summary>
public sealed class WarehouseScopeValidator(
    IExecutionContextAccessor contextAccessor,
    IWarehouseRepository warehouseRepository,
    IBranchRepository branchRepository) : IWarehouseScopeValidator
{
    private readonly IExecutionContextAccessor _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
    private readonly IWarehouseRepository _warehouseRepository = warehouseRepository ?? throw new ArgumentNullException(nameof(warehouseRepository));
    private readonly IBranchRepository _branchRepository = branchRepository ?? throw new ArgumentNullException(nameof(branchRepository));

    /// <inheritdoc />
    public async Task<Warehouse> ValidateAndGetWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default)
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

        return warehouse;
    }

    /// <inheritdoc />
    public async Task<Branch> ValidateAndGetBranchAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        if (branchId == Guid.Empty)
        {
            throw new ScopeUnresolvedException("Branch identifier cannot be empty.");
        }

        var current = _contextAccessor.Current
            ?? throw new ScopeUnresolvedException("Execution context is not established. Null scope never grants global access.");

        if (!current.CompanyId.HasValue || current.CompanyId.Value == Guid.Empty)
        {
            throw new ScopeUnresolvedException(
                $"Company scope is required to access branch '{branchId}', but current company scope is unresolved.");
        }

        var branch = await _branchRepository.GetByIdAsync(new BranchId(branchId), cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Branch), branchId);

        if (branch.CompanyId.Value != current.CompanyId.Value)
        {
            throw new ScopeMismatchException(
                $"Access denied: Branch '{branchId}' belongs to company '{branch.CompanyId.Value}', " +
                $"but caller scope is restricted to company '{current.CompanyId.Value}'.");
        }

        return branch;
    }

    /// <inheritdoc />
    public async Task<IReadOnlySet<BranchId>> GetAuthorizedBranchIdsAsync(CancellationToken cancellationToken = default)
    {
        var current = _contextAccessor.Current
            ?? throw new ScopeUnresolvedException("Execution context is not established. Null scope never grants global access.");

        if (!current.CompanyId.HasValue || current.CompanyId.Value == Guid.Empty)
        {
            throw new ScopeUnresolvedException(
                "Company scope is required to resolve authorized branches, but current company scope is unresolved.");
        }

        var companyBranches = await _branchRepository.GetByCompanyIdAsync(new CompanyId(current.CompanyId.Value), cancellationToken).ConfigureAwait(false);
        return companyBranches.Select(b => b.Id).ToHashSet();
    }
}
