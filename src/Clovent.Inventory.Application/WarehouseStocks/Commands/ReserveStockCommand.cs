using Clovent.Inventory.Application.WarehouseStocks.Dtos;
using Clovent.Inventory.WarehouseStocks;
using MediatR;

namespace Clovent.Inventory.Application.WarehouseStocks.Commands;

/// <summary>Reserves quantity against a warehouse balance. No transaction ledger entry - a reservation is an allocation, not a physical movement.</summary>
public sealed record ReserveStockCommand(Guid WarehouseStockId, decimal Quantity) : IRequest<WarehouseStockDto>;

/// <summary>Handles <see cref="ReserveStockCommand"/>.</summary>
public sealed class ReserveStockCommandHandler(IWarehouseStockRepository repository) : IRequestHandler<ReserveStockCommand, WarehouseStockDto>
{
    /// <inheritdoc/>
    public async Task<WarehouseStockDto> Handle(ReserveStockCommand request, CancellationToken cancellationToken)
    {
        var stock = await repository.GetByIdAsync(new WarehouseStockId(request.WarehouseStockId), cancellationToken)
            ?? throw new NotFoundException(nameof(WarehouseStock), request.WarehouseStockId);

        stock.Reserve(request.Quantity);
        return WarehouseStockDto.FromDomain(stock);
    }
}
