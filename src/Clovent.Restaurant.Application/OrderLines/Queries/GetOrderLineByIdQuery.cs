using Clovent.Restaurant.Application.OrderLines.Dtos;
using Clovent.Restaurant.OrderLines;
using MediatR;

namespace Clovent.Restaurant.Application.OrderLines.Queries;

/// <summary>Retrieves an order line by id.</summary>
public sealed record GetOrderLineByIdQuery(Guid OrderLineId) : IRequest<OrderLineDto>;

/// <summary>Handles <see cref="GetOrderLineByIdQuery"/>.</summary>
public sealed class GetOrderLineByIdQueryHandler(IOrderLineRepository repository) : IRequestHandler<GetOrderLineByIdQuery, OrderLineDto>
{
    /// <inheritdoc/>
    public async Task<OrderLineDto> Handle(GetOrderLineByIdQuery request, CancellationToken cancellationToken)
    {
        var line = await repository.GetByIdAsync(new OrderLineId(request.OrderLineId), cancellationToken)
            ?? throw new NotFoundException(nameof(OrderLine), request.OrderLineId);

        return OrderLineDto.FromDomain(line);
    }
}
