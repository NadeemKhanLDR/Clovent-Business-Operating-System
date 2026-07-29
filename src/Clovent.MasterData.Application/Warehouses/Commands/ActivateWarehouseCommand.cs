using Clovent.MasterData.Application.Warehouses.Dtos;
using Clovent.MasterData.Warehouses;
using MediatR;

namespace Clovent.MasterData.Application.Warehouses.Commands;

/// <summary>Activates a warehouse.</summary>
public sealed record ActivateWarehouseCommand(Guid WarehouseId) : IRequest<WarehouseDto>;

/// <summary>Handles <see cref="ActivateWarehouseCommand"/>.</summary>
public sealed class ActivateWarehouseCommandHandler(IWarehouseRepository warehouseRepository)
    : IRequestHandler<ActivateWarehouseCommand, WarehouseDto>
{
    /// <inheritdoc/>
    public async Task<WarehouseDto> Handle(ActivateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var warehouse = await warehouseRepository.GetByIdAsync(new WarehouseId(request.WarehouseId), cancellationToken)
            ?? throw new NotFoundException(nameof(Warehouse), request.WarehouseId);

        warehouse.Activate();

        return WarehouseDto.FromDomain(warehouse);
    }
}
