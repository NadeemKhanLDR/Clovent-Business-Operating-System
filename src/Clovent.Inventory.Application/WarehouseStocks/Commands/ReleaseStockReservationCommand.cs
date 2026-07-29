using Clovent.Inventory.Application.WarehouseStocks.Dtos;
using Clovent.Inventory.WarehouseStocks;
using MediatR;

namespace Clovent.Inventory.Application.WarehouseStocks.Commands;

/// <summary>Releases a previously-made reservation against a warehouse balance.</summary>
public sealed record ReleaseStockReservationCommand(Guid WarehouseStockId, decimal Quantity) : IRequest<WarehouseStockDto>;

/// <summary>Handles <see cref="ReleaseStockReservationCommand"/>.</summary>
public sealed class ReleaseStockReservationCommandHandler(IWarehouseStockRepository repository)
    : IRequestHandler<ReleaseStockReservationCommand, WarehouseStockDto>
{
    /// <inheritdoc/>
    public async Task<WarehouseStockDto> Handle(ReleaseStockReservationCommand request, CancellationToken cancellationToken)
    {
        var stock = await repository.GetByIdAsync(new WarehouseStockId(request.WarehouseStockId), cancellationToken)
            ?? throw new NotFoundException(nameof(WarehouseStock), request.WarehouseStockId);

        stock.Release(request.Quantity);
        return WarehouseStockDto.FromDomain(stock);
    }
}
