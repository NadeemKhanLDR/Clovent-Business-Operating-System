using Clovent.MasterData.Application.Warehouses.Dtos;
using Clovent.MasterData.Warehouses;
using MediatR;

namespace Clovent.MasterData.Application.Warehouses.Queries;

/// <summary>Retrieves a single warehouse by identity.</summary>
public sealed record GetWarehouseByIdQuery(Guid WarehouseId) : IRequest<WarehouseDto>;

/// <summary>Handles <see cref="GetWarehouseByIdQuery"/>.</summary>
public sealed class GetWarehouseByIdQueryHandler(IWarehouseRepository warehouseRepository)
    : IRequestHandler<GetWarehouseByIdQuery, WarehouseDto>
{
    /// <inheritdoc/>
    public async Task<WarehouseDto> Handle(GetWarehouseByIdQuery request, CancellationToken cancellationToken)
    {
        var warehouse = await warehouseRepository.GetByIdAsync(new WarehouseId(request.WarehouseId), cancellationToken)
            ?? throw new NotFoundException(nameof(Warehouse), request.WarehouseId);

        return WarehouseDto.FromDomain(warehouse);
    }
}
