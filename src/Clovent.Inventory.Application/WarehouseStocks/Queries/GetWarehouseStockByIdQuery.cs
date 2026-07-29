using Clovent.Inventory.Application.WarehouseStocks.Dtos;
using Clovent.Inventory.WarehouseStocks;
using MediatR;

namespace Clovent.Inventory.Application.WarehouseStocks.Queries;

/// <summary>Retrieves a single warehouse stock balance by identity.</summary>
public sealed record GetWarehouseStockByIdQuery(Guid WarehouseStockId) : IRequest<WarehouseStockDto>;

/// <summary>Handles <see cref="GetWarehouseStockByIdQuery"/>.</summary>
public sealed class GetWarehouseStockByIdQueryHandler(IWarehouseStockRepository repository)
    : IRequestHandler<GetWarehouseStockByIdQuery, WarehouseStockDto>
{
    /// <inheritdoc/>
    public async Task<WarehouseStockDto> Handle(GetWarehouseStockByIdQuery request, CancellationToken cancellationToken)
    {
        var stock = await repository.GetByIdAsync(new WarehouseStockId(request.WarehouseStockId), cancellationToken)
            ?? throw new NotFoundException(nameof(WarehouseStock), request.WarehouseStockId);

        return WarehouseStockDto.FromDomain(stock);
    }
}
