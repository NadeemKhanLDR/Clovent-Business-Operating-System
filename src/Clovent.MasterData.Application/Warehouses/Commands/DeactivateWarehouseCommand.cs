using Clovent.MasterData.Application.Warehouses.Dtos;
using Clovent.MasterData.Warehouses;
using MediatR;

namespace Clovent.MasterData.Application.Warehouses.Commands;

/// <summary>Deactivates a warehouse.</summary>
public sealed record DeactivateWarehouseCommand(Guid WarehouseId) : IRequest<WarehouseDto>;

/// <summary>Handles <see cref="DeactivateWarehouseCommand"/>.</summary>
public sealed class DeactivateWarehouseCommandHandler(IWarehouseRepository warehouseRepository)
    : IRequestHandler<DeactivateWarehouseCommand, WarehouseDto>
{
    /// <inheritdoc/>
    public async Task<WarehouseDto> Handle(DeactivateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var warehouse = await warehouseRepository.GetByIdAsync(new WarehouseId(request.WarehouseId), cancellationToken)
            ?? throw new NotFoundException(nameof(Warehouse), request.WarehouseId);

        warehouse.Deactivate();

        return WarehouseDto.FromDomain(warehouse);
    }
}
