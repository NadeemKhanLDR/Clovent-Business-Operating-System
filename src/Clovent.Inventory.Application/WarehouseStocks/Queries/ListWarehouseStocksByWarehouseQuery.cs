using Clovent.Inventory.Application.WarehouseStocks.Dtos;
using Clovent.Inventory.WarehouseStocks;
using Clovent.MasterData.Warehouses;
using MediatR;

namespace Clovent.Inventory.Application.WarehouseStocks.Queries;

/// <summary>Retrieves every stock balance at a warehouse.</summary>
public sealed record ListWarehouseStocksByWarehouseQuery(Guid WarehouseId) : IRequest<IReadOnlyCollection<WarehouseStockDto>>;

/// <summary>Handles <see cref="ListWarehouseStocksByWarehouseQuery"/>.</summary>
public sealed class ListWarehouseStocksByWarehouseQueryHandler(IWarehouseStockRepository repository)
    : IRequestHandler<ListWarehouseStocksByWarehouseQuery, IReadOnlyCollection<WarehouseStockDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<WarehouseStockDto>> Handle(ListWarehouseStocksByWarehouseQuery request, CancellationToken cancellationToken)
    {
        var stocks = await repository.GetByWarehouseIdAsync(new WarehouseId(request.WarehouseId), cancellationToken);
        return [.. stocks.Select(WarehouseStockDto.FromDomain)];
    }
}
