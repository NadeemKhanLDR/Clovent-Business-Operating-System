using Clovent.MasterData.Application.Warehouses.Dtos;
using Clovent.MasterData.Application.Warehouses.Security;
using Clovent.MasterData.Warehouses;
using Clovent.Platform.Execution;
using MediatR;

namespace Clovent.MasterData.Application.Warehouses.Queries;

/// <summary>Retrieves every warehouse belonging to the given branch, validating branch scope.</summary>
public sealed record ListWarehousesByBranchQuery(Guid BranchId) : IRequest<IReadOnlyCollection<WarehouseDto>>, IRequireCompanyScope;

/// <summary>Handles <see cref="ListWarehousesByBranchQuery"/>.</summary>
public sealed class ListWarehousesByBranchQueryHandler(
    IWarehouseRepository warehouseRepository,
    IWarehouseScopeValidator warehouseScopeValidator)
    : IRequestHandler<ListWarehousesByBranchQuery, IReadOnlyCollection<WarehouseDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<WarehouseDto>> Handle(ListWarehousesByBranchQuery request, CancellationToken cancellationToken)
    {
        var branch = await warehouseScopeValidator.ValidateAndGetBranchAsync(request.BranchId, cancellationToken);
        var warehouses = await warehouseRepository.GetByBranchIdAsync(branch.Id, cancellationToken);
        return [.. warehouses.Select(WarehouseDto.FromDomain)];
    }
}
