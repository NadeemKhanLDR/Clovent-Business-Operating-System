using Clovent.MasterData.Application.Warehouses.Dtos;
using Clovent.MasterData.Application.Warehouses.Security;
using Clovent.Platform.Execution;
using MediatR;

namespace Clovent.MasterData.Application.Warehouses.Commands;

/// <summary>Activates an authorized warehouse.</summary>
public sealed record ActivateWarehouseCommand(Guid WarehouseId) : IRequest<WarehouseDto>, IRequireCompanyScope;

/// <summary>Handles <see cref="ActivateWarehouseCommand"/>.</summary>
public sealed class ActivateWarehouseCommandHandler(IWarehouseScopeValidator warehouseScopeValidator)
    : IRequestHandler<ActivateWarehouseCommand, WarehouseDto>
{
    /// <inheritdoc/>
    public async Task<WarehouseDto> Handle(ActivateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var warehouse = await warehouseScopeValidator.ValidateAndGetWarehouseAsync(request.WarehouseId, cancellationToken);

        warehouse.Activate();

        return WarehouseDto.FromDomain(warehouse);
    }
}
