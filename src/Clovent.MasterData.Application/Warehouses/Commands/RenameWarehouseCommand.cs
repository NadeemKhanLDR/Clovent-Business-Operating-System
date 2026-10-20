using Clovent.MasterData.Application.Warehouses.Dtos;
using Clovent.MasterData.Application.Warehouses.Security;
using Clovent.MasterData.Warehouses.ValueObjects;
using Clovent.Platform.Execution;
using MediatR;

namespace Clovent.MasterData.Application.Warehouses.Commands;

/// <summary>Renames an authorized warehouse.</summary>
public sealed record RenameWarehouseCommand(Guid WarehouseId, string Name) : IRequest<WarehouseDto>, IRequireCompanyScope;

/// <summary>Handles <see cref="RenameWarehouseCommand"/>.</summary>
public sealed class RenameWarehouseCommandHandler(IWarehouseScopeValidator warehouseScopeValidator)
    : IRequestHandler<RenameWarehouseCommand, WarehouseDto>
{
    /// <inheritdoc/>
    public async Task<WarehouseDto> Handle(RenameWarehouseCommand request, CancellationToken cancellationToken)
    {
        var warehouse = await warehouseScopeValidator.ValidateAndGetWarehouseAsync(request.WarehouseId, cancellationToken);

        warehouse.Rename(WarehouseName.Create(request.Name));

        return WarehouseDto.FromDomain(warehouse);
    }
}
