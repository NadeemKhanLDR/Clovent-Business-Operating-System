using Clovent.Restaurant.Application.OrderLines.Dtos;
using Clovent.Restaurant.OrderLines;
using MediatR;

namespace Clovent.Restaurant.Application.OrderLines.Commands;

/// <summary>Restores a voided order line.</summary>
public sealed record UnvoidOrderLineCommand(Guid OrderLineId) : IRequest<OrderLineDto>;

/// <summary>Handles <see cref="UnvoidOrderLineCommand"/>.</summary>
public sealed class UnvoidOrderLineCommandHandler(IOrderLineRepository repository) : IRequestHandler<UnvoidOrderLineCommand, OrderLineDto>
{
    /// <inheritdoc/>
    public async Task<OrderLineDto> Handle(UnvoidOrderLineCommand request, CancellationToken cancellationToken)
    {
        var line = await repository.GetByIdAsync(new OrderLineId(request.OrderLineId), cancellationToken)
            ?? throw new NotFoundException(nameof(OrderLine), request.OrderLineId);

        line.Unvoid();
        return OrderLineDto.FromDomain(line);
    }
}
