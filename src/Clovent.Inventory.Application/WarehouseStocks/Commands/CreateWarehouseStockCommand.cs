using Clovent.Catalog.Variants;
using Clovent.Inventory.Application.WarehouseStocks.Dtos;
using Clovent.Inventory.WarehouseStocks;
using Clovent.MasterData.Warehouses;
using MediatR;

namespace Clovent.Inventory.Application.WarehouseStocks.Commands;

/// <summary>Creates a new, zero-balance stock record for a warehouse/variant pair.</summary>
public sealed record CreateWarehouseStockCommand(
    Guid WarehouseId,
    Guid ProductVariantId,
    decimal MinimumStock = 0,
    decimal MaximumStock = 0,
    bool AllowNegativeStock = false) : IRequest<WarehouseStockDto>;

/// <summary>Handles <see cref="CreateWarehouseStockCommand"/>.</summary>
public sealed class CreateWarehouseStockCommandHandler(IWarehouseStockRepository repository)
    : IRequestHandler<CreateWarehouseStockCommand, WarehouseStockDto>
{
    /// <inheritdoc/>
    public async Task<WarehouseStockDto> Handle(CreateWarehouseStockCommand request, CancellationToken cancellationToken)
    {
        var stock = WarehouseStock.Create(
            new WarehouseId(request.WarehouseId),
            new ProductVariantId(request.ProductVariantId),
            request.MinimumStock,
            request.MaximumStock,
            request.AllowNegativeStock);

        await repository.AddAsync(stock, cancellationToken);

        return WarehouseStockDto.FromDomain(stock);
    }
}
