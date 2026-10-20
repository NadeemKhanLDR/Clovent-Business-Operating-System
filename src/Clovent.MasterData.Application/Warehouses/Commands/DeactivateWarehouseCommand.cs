using Clovent.MasterData.Application.Warehouses.Dtos;
using Clovent.MasterData.Application.Warehouses.Security;
using Clovent.Platform.Execution;
using MediatR;

namespace Clovent.MasterData.Application.Warehouses.Commands;

/// <summary>Deactivates an authorized warehouse.</summary>
public sealed record DeactivateWarehouseCommand(Guid WarehouseId) : IRequest<WarehouseDto>, IRequireCompanyScope;

/// <summary>Handles <see cref="DeactivateWarehouseCommand"/>.</summary>
public sealed class DeactivateWarehouseCommandHandler(IWarehouseScopeValidator warehouseScopeValidator)
    : IRequestHandler<DeactivateWarehouseCommand, WarehouseDto>
{
    /// <inheritdoc/>
    public async Task<WarehouseDto> Handle(DeactivateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var warehouse = await warehouseScopeValidator.ValidateAndGetWarehouseAsync(request.WarehouseId, cancellationToken);

        warehouse.Deactivate();

        return WarehouseDto.FromDomain(warehouse);
    }
}
