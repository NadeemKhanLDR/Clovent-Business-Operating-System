using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Orders;
using MediatR;

namespace Clovent.Restaurant.Application.Orders.Queries;

/// <summary>Retrieves the restaurant's current order-number prefix/next-number, for the Restaurant Setup screen. Creates the sequence with its defaults if no order has ever been created yet.</summary>
public sealed record GetOrderNumberSequenceQuery : IRequest<OrderNumberSequenceDto>;

/// <summary>Handles <see cref="GetOrderNumberSequenceQuery"/>.</summary>
public sealed class GetOrderNumberSequenceQueryHandler(IOrderNumberSequenceRepository repository)
    : IRequestHandler<GetOrderNumberSequenceQuery, OrderNumberSequenceDto>
{
    /// <inheritdoc/>
    public async Task<OrderNumberSequenceDto> Handle(GetOrderNumberSequenceQuery request, CancellationToken cancellationToken)
    {
        var sequence = await repository.GetSingletonAsync(cancellationToken);
        if (sequence is null)
        {
            sequence = OrderNumberSequence.CreateDefault();
            await repository.AddAsync(sequence, cancellationToken);
        }

        return OrderNumberSequenceDto.FromDomain(sequence);
    }
}
