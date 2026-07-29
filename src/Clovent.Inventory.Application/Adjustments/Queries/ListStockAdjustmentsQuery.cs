using Clovent.Inventory.Adjustments;
using Clovent.Inventory.Application.Adjustments.Dtos;
using MediatR;

namespace Clovent.Inventory.Application.Adjustments.Queries;

/// <summary>Retrieves every stock adjustment.</summary>
public sealed record ListStockAdjustmentsQuery : IRequest<IReadOnlyCollection<StockAdjustmentDto>>;

/// <summary>Handles <see cref="ListStockAdjustmentsQuery"/>.</summary>
public sealed class ListStockAdjustmentsQueryHandler(IStockAdjustmentRepository repository)
    : IRequestHandler<ListStockAdjustmentsQuery, IReadOnlyCollection<StockAdjustmentDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<StockAdjustmentDto>> Handle(ListStockAdjustmentsQuery request, CancellationToken cancellationToken)
    {
        var adjustments = await repository.GetAllAsync(cancellationToken);
        return [.. adjustments.Select(StockAdjustmentDto.FromDomain)];
    }
}
