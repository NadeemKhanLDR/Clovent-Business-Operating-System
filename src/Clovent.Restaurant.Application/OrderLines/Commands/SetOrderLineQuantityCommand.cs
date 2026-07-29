using Clovent.Restaurant.Application.OrderLines.Dtos;
using Clovent.Restaurant.OrderLines;
using MediatR;

namespace Clovent.Restaurant.Application.OrderLines.Commands;

/// <summary>Changes an order line's quantity.</summary>
public sealed record SetOrderLineQuantityCommand(Guid OrderLineId, decimal Quantity) : IRequest<OrderLineDto>;

/// <summary>Handles <see cref="SetOrderLineQuantityCommand"/>.</summary>
public sealed class SetOrderLineQuantityCommandHandler(IOrderLineRepository repository) : IRequestHandler<SetOrderLineQuantityCommand, OrderLineDto>
{
    /// <inheritdoc/>
    public async Task<OrderLineDto> Handle(SetOrderLineQuantityCommand request, CancellationToken cancellationToken)
    {
        var line = await repository.GetByIdAsync(new OrderLineId(request.OrderLineId), cancellationToken)
            ?? throw new NotFoundException(nameof(OrderLine), request.OrderLineId);

        line.SetQuantity(request.Quantity);
        return OrderLineDto.FromDomain(line);
    }
}
