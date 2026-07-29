using Clovent.Identity.Branches;
using Clovent.MasterData.Application.Warehouses.Dtos;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Warehouses;
using Clovent.MasterData.Warehouses.ValueObjects;
using MediatR;

namespace Clovent.MasterData.Application.Warehouses.Commands;

/// <summary>Creates a new warehouse under an existing branch.</summary>
public sealed record CreateWarehouseCommand(Guid BranchId, string Name, string Code) : IRequest<WarehouseDto>;

/// <summary>Handles <see cref="CreateWarehouseCommand"/>.</summary>
public sealed class CreateWarehouseCommandHandler(IWarehouseRepository warehouseRepository)
    : IRequestHandler<CreateWarehouseCommand, WarehouseDto>
{
    /// <inheritdoc/>
    public async Task<WarehouseDto> Handle(CreateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var warehouse = Warehouse.Create(new BranchId(request.BranchId), WarehouseName.Create(request.Name), EntityCode.Create(request.Code));

        await warehouseRepository.AddAsync(warehouse, cancellationToken);

        return WarehouseDto.FromDomain(warehouse);
    }
}
