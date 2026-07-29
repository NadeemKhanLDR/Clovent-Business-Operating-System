using Clovent.Inventory.Application.Transfers.Dtos;
using Clovent.Inventory.Transfers;
using MediatR;

namespace Clovent.Inventory.Application.Transfers.Commands;

/// <summary>Cancels a pending stock transfer before it is completed.</summary>
public sealed record CancelStockTransferCommand(Guid StockTransferId) : IRequest<StockTransferDto>;

/// <summary>Handles <see cref="CancelStockTransferCommand"/>.</summary>
public sealed class CancelStockTransferCommandHandler(IStockTransferRepository repository)
    : IRequestHandler<CancelStockTransferCommand, StockTransferDto>
{
    /// <inheritdoc/>
    public async Task<StockTransferDto> Handle(CancelStockTransferCommand request, CancellationToken cancellationToken)
    {
        var transfer = await repository.GetByIdAsync(new StockTransferId(request.StockTransferId), cancellationToken)
            ?? throw new NotFoundException(nameof(StockTransfer), request.StockTransferId);

        transfer.Cancel();
        return StockTransferDto.FromDomain(transfer);
    }
}
