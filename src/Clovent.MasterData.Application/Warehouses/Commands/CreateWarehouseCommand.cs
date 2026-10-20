using Clovent.Identity.Branches;
using Clovent.MasterData.Application.Warehouses.Dtos;
using Clovent.MasterData.Application.Warehouses.Security;
using Clovent.MasterData.Shared.ValueObjects;
using Clovent.MasterData.Warehouses;
using Clovent.MasterData.Warehouses.ValueObjects;
using Clovent.Platform.Execution;
using MediatR;

namespace Clovent.MasterData.Application.Warehouses.Commands;

/// <summary>Creates a new warehouse under an authorized branch.</summary>
public sealed record CreateWarehouseCommand(Guid BranchId, string Name, string Code) : IRequest<WarehouseDto>, IRequireCompanyScope;

/// <summary>Handles <see cref="CreateWarehouseCommand"/>.</summary>
public sealed class CreateWarehouseCommandHandler(
    IWarehouseRepository warehouseRepository,
    IWarehouseScopeValidator warehouseScopeValidator)
    : IRequestHandler<CreateWarehouseCommand, WarehouseDto>
{
    /// <inheritdoc/>
    public async Task<WarehouseDto> Handle(CreateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var branch = await warehouseScopeValidator.ValidateAndGetBranchAsync(request.BranchId, cancellationToken);
        var warehouse = Warehouse.Create(branch.Id, WarehouseName.Create(request.Name), EntityCode.Create(request.Code));

        await warehouseRepository.AddAsync(warehouse, cancellationToken);

        return WarehouseDto.FromDomain(warehouse);
    }
}
