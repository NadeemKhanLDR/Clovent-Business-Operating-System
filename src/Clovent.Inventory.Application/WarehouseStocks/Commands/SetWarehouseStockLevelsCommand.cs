using Clovent.Inventory.Application.WarehouseStocks.Dtos;
using Clovent.Inventory.WarehouseStocks;
using MediatR;

namespace Clovent.Inventory.Application.WarehouseStocks.Commands;

/// <summary>Sets a warehouse balance's minimum/maximum stock levels.</summary>
public sealed record SetWarehouseStockLevelsCommand(Guid WarehouseStockId, decimal MinimumStock, decimal MaximumStock) : IRequest<WarehouseStockDto>;

/// <summary>Handles <see cref="SetWarehouseStockLevelsCommand"/>.</summary>
public sealed class SetWarehouseStockLevelsCommandHandler(IWarehouseStockRepository repository)
    : IRequestHandler<SetWarehouseStockLevelsCommand, WarehouseStockDto>
{
    /// <inheritdoc/>
    public async Task<WarehouseStockDto> Handle(SetWarehouseStockLevelsCommand request, CancellationToken cancellationToken)
    {
        var stock = await repository.GetByIdAsync(new WarehouseStockId(request.WarehouseStockId), cancellationToken)
            ?? throw new NotFoundException(nameof(WarehouseStock), request.WarehouseStockId);

        stock.SetStockLevels(request.MinimumStock, request.MaximumStock);
        return WarehouseStockDto.FromDomain(stock);
    }
}
