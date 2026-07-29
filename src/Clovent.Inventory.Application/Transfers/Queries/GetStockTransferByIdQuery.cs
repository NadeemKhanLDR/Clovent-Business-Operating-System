using Clovent.Inventory.Application.Transfers.Dtos;
using Clovent.Inventory.Transfers;
using MediatR;

namespace Clovent.Inventory.Application.Transfers.Queries;

/// <summary>Retrieves a single stock transfer by identity.</summary>
public sealed record GetStockTransferByIdQuery(Guid StockTransferId) : IRequest<StockTransferDto>;

/// <summary>Handles <see cref="GetStockTransferByIdQuery"/>.</summary>
public sealed class GetStockTransferByIdQueryHandler(IStockTransferRepository repository)
    : IRequestHandler<GetStockTransferByIdQuery, StockTransferDto>
{
    /// <inheritdoc/>
    public async Task<StockTransferDto> Handle(GetStockTransferByIdQuery request, CancellationToken cancellationToken)
    {
        var transfer = await repository.GetByIdAsync(new StockTransferId(request.StockTransferId), cancellationToken)
            ?? throw new NotFoundException(nameof(StockTransfer), request.StockTransferId);

        return StockTransferDto.FromDomain(transfer);
    }
}
