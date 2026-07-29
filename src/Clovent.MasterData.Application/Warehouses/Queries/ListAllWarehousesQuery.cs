using Clovent.MasterData.Application.Warehouses.Dtos;
using Clovent.MasterData.Warehouses;
using MediatR;

namespace Clovent.MasterData.Application.Warehouses.Queries;

/// <summary>Retrieves every warehouse across every branch - used by Milestone 14 ("Product Catalog &amp; Inventory Foundation") Inventory screens, which scope by warehouse directly.</summary>
public sealed record ListAllWarehousesQuery : IRequest<IReadOnlyCollection<WarehouseDto>>;

/// <summary>Handles <see cref="ListAllWarehousesQuery"/>.</summary>
public sealed class ListAllWarehousesQueryHandler(IWarehouseRepository warehouseRepository)
    : IRequestHandler<ListAllWarehousesQuery, IReadOnlyCollection<WarehouseDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<WarehouseDto>> Handle(ListAllWarehousesQuery request, CancellationToken cancellationToken)
    {
        var warehouses = await warehouseRepository.GetAllAsync(cancellationToken);
        return [.. warehouses.Select(WarehouseDto.FromDomain)];
    }
}
