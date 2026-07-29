using Clovent.MasterData.Application.Warehouses.Dtos;
using Clovent.MasterData.Warehouses;
using Clovent.MasterData.Warehouses.ValueObjects;
using MediatR;

namespace Clovent.MasterData.Application.Warehouses.Commands;

/// <summary>Renames an existing warehouse.</summary>
public sealed record RenameWarehouseCommand(Guid WarehouseId, string Name) : IRequest<WarehouseDto>;

/// <summary>Handles <see cref="RenameWarehouseCommand"/>.</summary>
public sealed class RenameWarehouseCommandHandler(IWarehouseRepository warehouseRepository)
    : IRequestHandler<RenameWarehouseCommand, WarehouseDto>
{
    /// <inheritdoc/>
    public async Task<WarehouseDto> Handle(RenameWarehouseCommand request, CancellationToken cancellationToken)
    {
        var warehouse = await warehouseRepository.GetByIdAsync(new WarehouseId(request.WarehouseId), cancellationToken)
            ?? throw new NotFoundException(nameof(Warehouse), request.WarehouseId);

        warehouse.Rename(WarehouseName.Create(request.Name));

        return WarehouseDto.FromDomain(warehouse);
    }
}
