using Clovent.Catalog.Variants;
using Clovent.Inventory.Application.Transfers.Dtos;
using Clovent.Inventory.Transfers;
using Clovent.MasterData.Warehouses;
using MediatR;

namespace Clovent.Inventory.Application.Transfers.Commands;

/// <summary>Proposes a new, pending stock transfer.</summary>
public sealed record CreateStockTransferCommand(Guid SourceWarehouseId, Guid DestinationWarehouseId, Guid ProductVariantId, decimal Quantity)
    : IRequest<StockTransferDto>;

/// <summary>Handles <see cref="CreateStockTransferCommand"/>.</summary>
public sealed class CreateStockTransferCommandHandler(IStockTransferRepository repository)
    : IRequestHandler<CreateStockTransferCommand, StockTransferDto>
{
    /// <inheritdoc/>
    public async Task<StockTransferDto> Handle(CreateStockTransferCommand request, CancellationToken cancellationToken)
    {
        var transfer = StockTransfer.Create(
            new WarehouseId(request.SourceWarehouseId),
            new WarehouseId(request.DestinationWarehouseId),
            new ProductVariantId(request.ProductVariantId),
            request.Quantity);

        await repository.AddAsync(transfer, cancellationToken);

        return StockTransferDto.FromDomain(transfer);
    }
}
