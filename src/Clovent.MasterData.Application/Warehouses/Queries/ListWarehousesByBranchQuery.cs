using Clovent.Identity.Branches;
using Clovent.MasterData.Application.Warehouses.Dtos;
using Clovent.MasterData.Warehouses;
using MediatR;

namespace Clovent.MasterData.Application.Warehouses.Queries;

/// <summary>Retrieves every warehouse belonging to the given branch.</summary>
public sealed record ListWarehousesByBranchQuery(Guid BranchId) : IRequest<IReadOnlyCollection<WarehouseDto>>;

/// <summary>Handles <see cref="ListWarehousesByBranchQuery"/>.</summary>
public sealed class ListWarehousesByBranchQueryHandler(IWarehouseRepository warehouseRepository)
    : IRequestHandler<ListWarehousesByBranchQuery, IReadOnlyCollection<WarehouseDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<WarehouseDto>> Handle(ListWarehousesByBranchQuery request, CancellationToken cancellationToken)
    {
        var warehouses = await warehouseRepository.GetByBranchIdAsync(new BranchId(request.BranchId), cancellationToken);
        return [.. warehouses.Select(WarehouseDto.FromDomain)];
    }
}
