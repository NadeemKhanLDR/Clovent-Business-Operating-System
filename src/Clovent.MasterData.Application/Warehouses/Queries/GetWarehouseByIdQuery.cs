using Clovent.MasterData.Application.Warehouses.Dtos;
using Clovent.MasterData.Application.Warehouses.Security;
using Clovent.Platform.Execution;
using MediatR;

namespace Clovent.MasterData.Application.Warehouses.Queries;

/// <summary>Retrieves a single warehouse by identity, authoritatively validating active company scope.</summary>
public sealed record GetWarehouseByIdQuery(Guid WarehouseId) : IRequest<WarehouseDto>, IRequireCompanyScope;

/// <summary>Handles <see cref="GetWarehouseByIdQuery"/>.</summary>
public sealed class GetWarehouseByIdQueryHandler(IWarehouseScopeValidator warehouseScopeValidator)
    : IRequestHandler<GetWarehouseByIdQuery, WarehouseDto>
{
    /// <inheritdoc/>
    public async Task<WarehouseDto> Handle(GetWarehouseByIdQuery request, CancellationToken cancellationToken)
    {
        var warehouse = await warehouseScopeValidator.ValidateAndGetWarehouseAsync(request.WarehouseId, cancellationToken);
        return WarehouseDto.FromDomain(warehouse);
    }
}
