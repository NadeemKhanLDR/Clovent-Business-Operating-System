using Clovent.MasterData.Application.Warehouses.Dtos;
using Clovent.MasterData.Application.Warehouses.Security;
using Clovent.MasterData.Warehouses;
using Clovent.Platform.Execution;
using MediatR;

namespace Clovent.MasterData.Application.Warehouses.Queries;

/// <summary>Retrieves warehouses across authorized branches within the caller's active execution scope.</summary>
public sealed record ListAllWarehousesQuery : IRequest<IReadOnlyCollection<WarehouseDto>>, IRequireCompanyScope;

/// <summary>Handles <see cref="ListAllWarehousesQuery"/>.</summary>
public sealed class ListAllWarehousesQueryHandler(
    IWarehouseRepository warehouseRepository,
    IWarehouseScopeValidator warehouseScopeValidator)
    : IRequestHandler<ListAllWarehousesQuery, IReadOnlyCollection<WarehouseDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<WarehouseDto>> Handle(ListAllWarehousesQuery request, CancellationToken cancellationToken)
    {
        var authorizedBranchIds = await warehouseScopeValidator.GetAuthorizedBranchIdsAsync(cancellationToken);
        var allWarehouses = await warehouseRepository.GetAllAsync(cancellationToken);
        var authorizedWarehouses = allWarehouses.Where(w => authorizedBranchIds.Contains(w.BranchId));
        return [.. authorizedWarehouses.Select(WarehouseDto.FromDomain)];
    }
}
