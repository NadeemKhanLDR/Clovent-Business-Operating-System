using Clovent.Inventory.Application.WarehouseStocks.Dtos;
using Clovent.Inventory.WarehouseStocks;
using MediatR;

namespace Clovent.Inventory.Application.WarehouseStocks.Commands;

/// <summary>Sets whether a warehouse balance may go negative when issuing.</summary>
public sealed record SetNegativeStockPolicyCommand(Guid WarehouseStockId, bool AllowNegativeStock) : IRequest<WarehouseStockDto>;

/// <summary>Handles <see cref="SetNegativeStockPolicyCommand"/>.</summary>
public sealed class SetNegativeStockPolicyCommandHandler(IWarehouseStockRepository repository)
    : IRequestHandler<SetNegativeStockPolicyCommand, WarehouseStockDto>
{
    /// <inheritdoc/>
    public async Task<WarehouseStockDto> Handle(SetNegativeStockPolicyCommand request, CancellationToken cancellationToken)
    {
        var stock = await repository.GetByIdAsync(new WarehouseStockId(request.WarehouseStockId), cancellationToken)
            ?? throw new NotFoundException(nameof(WarehouseStock), request.WarehouseStockId);

        stock.SetNegativeStockPolicy(request.AllowNegativeStock);
        return WarehouseStockDto.FromDomain(stock);
    }
}
