using Clovent.Inventory.Adjustments;
using Clovent.Inventory.Application.Adjustments.Dtos;
using MediatR;

namespace Clovent.Inventory.Application.Adjustments.Queries;

/// <summary>Retrieves a single stock adjustment by identity.</summary>
public sealed record GetStockAdjustmentByIdQuery(Guid StockAdjustmentId) : IRequest<StockAdjustmentDto>;

/// <summary>Handles <see cref="GetStockAdjustmentByIdQuery"/>.</summary>
public sealed class GetStockAdjustmentByIdQueryHandler(IStockAdjustmentRepository repository)
    : IRequestHandler<GetStockAdjustmentByIdQuery, StockAdjustmentDto>
{
    /// <inheritdoc/>
    public async Task<StockAdjustmentDto> Handle(GetStockAdjustmentByIdQuery request, CancellationToken cancellationToken)
    {
        var adjustment = await repository.GetByIdAsync(new StockAdjustmentId(request.StockAdjustmentId), cancellationToken)
            ?? throw new NotFoundException(nameof(StockAdjustment), request.StockAdjustmentId);

        return StockAdjustmentDto.FromDomain(adjustment);
    }
}
