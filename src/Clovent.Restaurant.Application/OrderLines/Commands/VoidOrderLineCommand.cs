using Clovent.Restaurant.Application.OrderLines.Dtos;
using Clovent.Restaurant.OrderLines;
using MediatR;

namespace Clovent.Restaurant.Application.OrderLines.Commands;

/// <summary>Voids an order line - excludes it from totals and future kitchen tickets without removing it from the order.</summary>
public sealed record VoidOrderLineCommand(Guid OrderLineId) : IRequest<OrderLineDto>;

/// <summary>Handles <see cref="VoidOrderLineCommand"/>.</summary>
public sealed class VoidOrderLineCommandHandler(IOrderLineRepository repository) : IRequestHandler<VoidOrderLineCommand, OrderLineDto>
{
    /// <inheritdoc/>
    public async Task<OrderLineDto> Handle(VoidOrderLineCommand request, CancellationToken cancellationToken)
    {
        var line = await repository.GetByIdAsync(new OrderLineId(request.OrderLineId), cancellationToken)
            ?? throw new NotFoundException(nameof(OrderLine), request.OrderLineId);

        line.Void();
        return OrderLineDto.FromDomain(line);
    }
}
