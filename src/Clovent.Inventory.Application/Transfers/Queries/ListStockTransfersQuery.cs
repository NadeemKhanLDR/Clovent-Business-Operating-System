using Clovent.Inventory.Application.Transfers.Dtos;
using Clovent.Inventory.Transfers;
using MediatR;

namespace Clovent.Inventory.Application.Transfers.Queries;

/// <summary>Retrieves every stock transfer.</summary>
public sealed record ListStockTransfersQuery : IRequest<IReadOnlyCollection<StockTransferDto>>;

/// <summary>Handles <see cref="ListStockTransfersQuery"/>.</summary>
public sealed class ListStockTransfersQueryHandler(IStockTransferRepository repository)
    : IRequestHandler<ListStockTransfersQuery, IReadOnlyCollection<StockTransferDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<StockTransferDto>> Handle(ListStockTransfersQuery request, CancellationToken cancellationToken)
    {
        var transfers = await repository.GetAllAsync(cancellationToken);
        return [.. transfers.Select(StockTransferDto.FromDomain)];
    }
}
