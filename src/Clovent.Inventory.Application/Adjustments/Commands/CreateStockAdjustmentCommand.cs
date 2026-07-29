using Clovent.Catalog.Variants;
using Clovent.Inventory.Adjustments;
using Clovent.Inventory.Application.Adjustments.Dtos;
using Clovent.MasterData.Warehouses;
using MediatR;

namespace Clovent.Inventory.Application.Adjustments.Commands;

/// <summary>Proposes a new, pending stock adjustment.</summary>
public sealed record CreateStockAdjustmentCommand(Guid WarehouseId, Guid ProductVariantId, StockAdjustmentType AdjustmentType, decimal Quantity, string Reason)
    : IRequest<StockAdjustmentDto>;

/// <summary>Handles <see cref="CreateStockAdjustmentCommand"/>.</summary>
public sealed class CreateStockAdjustmentCommandHandler(IStockAdjustmentRepository repository)
    : IRequestHandler<CreateStockAdjustmentCommand, StockAdjustmentDto>
{
    /// <inheritdoc/>
    public async Task<StockAdjustmentDto> Handle(CreateStockAdjustmentCommand request, CancellationToken cancellationToken)
    {
        var adjustment = StockAdjustment.Create(
            new WarehouseId(request.WarehouseId),
            new ProductVariantId(request.ProductVariantId),
            request.AdjustmentType,
            request.Quantity,
            request.Reason);

        await repository.AddAsync(adjustment, cancellationToken);

        return StockAdjustmentDto.FromDomain(adjustment);
    }
}
