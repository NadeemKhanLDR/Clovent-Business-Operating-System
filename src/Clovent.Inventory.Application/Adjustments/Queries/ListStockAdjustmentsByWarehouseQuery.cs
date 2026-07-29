using Clovent.Inventory.Adjustments;
using Clovent.Inventory.Application.Adjustments.Dtos;
using Clovent.MasterData.Warehouses;
using MediatR;

namespace Clovent.Inventory.Application.Adjustments.Queries;

/// <summary>Retrieves every adjustment proposed for a warehouse.</summary>
public sealed record ListStockAdjustmentsByWarehouseQuery(Guid WarehouseId) : IRequest<IReadOnlyCollection<StockAdjustmentDto>>;

/// <summary>Handles <see cref="ListStockAdjustmentsByWarehouseQuery"/>.</summary>
public sealed class ListStockAdjustmentsByWarehouseQueryHandler(IStockAdjustmentRepository repository)
    : IRequestHandler<ListStockAdjustmentsByWarehouseQuery, IReadOnlyCollection<StockAdjustmentDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<StockAdjustmentDto>> Handle(ListStockAdjustmentsByWarehouseQuery request, CancellationToken cancellationToken)
    {
        var adjustments = await repository.GetByWarehouseIdAsync(new WarehouseId(request.WarehouseId), cancellationToken);
        return [.. adjustments.Select(StockAdjustmentDto.FromDomain)];
    }
}
