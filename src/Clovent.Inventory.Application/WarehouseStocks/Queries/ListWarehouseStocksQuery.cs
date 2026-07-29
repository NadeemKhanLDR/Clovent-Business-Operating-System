using Clovent.Inventory.Application.WarehouseStocks.Dtos;
using Clovent.Inventory.WarehouseStocks;
using MediatR;

namespace Clovent.Inventory.Application.WarehouseStocks.Queries;

/// <summary>Retrieves every stock balance across every warehouse.</summary>
public sealed record ListWarehouseStocksQuery : IRequest<IReadOnlyCollection<WarehouseStockDto>>;

/// <summary>Handles <see cref="ListWarehouseStocksQuery"/>.</summary>
public sealed class ListWarehouseStocksQueryHandler(IWarehouseStockRepository repository)
    : IRequestHandler<ListWarehouseStocksQuery, IReadOnlyCollection<WarehouseStockDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<WarehouseStockDto>> Handle(ListWarehouseStocksQuery request, CancellationToken cancellationToken)
    {
        var stocks = await repository.GetAllAsync(cancellationToken);
        return [.. stocks.Select(WarehouseStockDto.FromDomain)];
    }
}
