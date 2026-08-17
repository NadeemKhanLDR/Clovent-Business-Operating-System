using Clovent.Restaurant.Application.OrderLines.Dtos;
using Clovent.Restaurant.OrderLines;
using MediatR;

namespace Clovent.Restaurant.Application.OrderLines.Commands;

/// <summary>
/// Overrides an order line's unit price - "Half Chicken Karahi, make it 300
/// this time" - away from the price snapshotted from the catalog when the
/// line was added. <paramref name="PerformedBy"/> is a display name/username
/// supplied by the caller (the Desktop layer's <c>ICurrentSession</c>), not
/// resolved here - the Restaurant domain has no notion of "current user".
/// </summary>
public sealed record OverrideOrderLinePriceCommand(Guid OrderLineId, decimal NewUnitPrice, string Reason, string PerformedBy) : IRequest<OrderLineDto>;

/// <summary>Handles <see cref="OverrideOrderLinePriceCommand"/>.</summary>
public sealed class OverrideOrderLinePriceCommandHandler(IOrderLineRepository repository) : IRequestHandler<OverrideOrderLinePriceCommand, OrderLineDto>
{
    /// <inheritdoc/>
    public async Task<OrderLineDto> Handle(OverrideOrderLinePriceCommand request, CancellationToken cancellationToken)
    {
        var line = await repository.GetByIdAsync(new OrderLineId(request.OrderLineId), cancellationToken)
            ?? throw new NotFoundException(nameof(OrderLine), request.OrderLineId);

        line.OverridePrice(request.NewUnitPrice, request.Reason, request.PerformedBy);
        return OrderLineDto.FromDomain(line);
    }
}
