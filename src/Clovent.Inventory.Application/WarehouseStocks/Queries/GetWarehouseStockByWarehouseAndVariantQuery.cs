using Clovent.Catalog.Variants;
using Clovent.Inventory.Application.WarehouseStocks.Dtos;
using Clovent.Inventory.WarehouseStocks;
using Clovent.MasterData.Warehouses;
using MediatR;

namespace Clovent.Inventory.Application.WarehouseStocks.Queries;

/// <summary>Retrieves the stock balance for a specific warehouse/variant pair, or <see langword="null"/> if none exists yet.</summary>
public sealed record GetWarehouseStockByWarehouseAndVariantQuery(Guid WarehouseId, Guid ProductVariantId) : IRequest<WarehouseStockDto?>;

/// <summary>Handles <see cref="GetWarehouseStockByWarehouseAndVariantQuery"/>.</summary>
public sealed class GetWarehouseStockByWarehouseAndVariantQueryHandler(IWarehouseStockRepository repository)
    : IRequestHandler<GetWarehouseStockByWarehouseAndVariantQuery, WarehouseStockDto?>
{
    /// <inheritdoc/>
    public async Task<WarehouseStockDto?> Handle(GetWarehouseStockByWarehouseAndVariantQuery request, CancellationToken cancellationToken)
    {
        var stock = await repository.GetByWarehouseAndVariantAsync(
            new WarehouseId(request.WarehouseId), new ProductVariantId(request.ProductVariantId), cancellationToken);

        return stock is null ? null : WarehouseStockDto.FromDomain(stock);
    }
}
