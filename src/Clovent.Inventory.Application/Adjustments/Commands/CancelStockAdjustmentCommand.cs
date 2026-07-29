using Clovent.Inventory.Adjustments;
using Clovent.Inventory.Application.Adjustments.Dtos;
using MediatR;

namespace Clovent.Inventory.Application.Adjustments.Commands;

/// <summary>Cancels a pending stock adjustment before it is applied.</summary>
public sealed record CancelStockAdjustmentCommand(Guid StockAdjustmentId) : IRequest<StockAdjustmentDto>;

/// <summary>Handles <see cref="CancelStockAdjustmentCommand"/>.</summary>
public sealed class CancelStockAdjustmentCommandHandler(IStockAdjustmentRepository repository)
    : IRequestHandler<CancelStockAdjustmentCommand, StockAdjustmentDto>
{
    /// <inheritdoc/>
    public async Task<StockAdjustmentDto> Handle(CancelStockAdjustmentCommand request, CancellationToken cancellationToken)
    {
        var adjustment = await repository.GetByIdAsync(new StockAdjustmentId(request.StockAdjustmentId), cancellationToken)
            ?? throw new NotFoundException(nameof(StockAdjustment), request.StockAdjustmentId);

        adjustment.Cancel();
        return StockAdjustmentDto.FromDomain(adjustment);
    }
}
