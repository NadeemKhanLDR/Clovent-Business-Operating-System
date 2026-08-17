using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Orders;
using MediatR;

namespace Clovent.Restaurant.Application.Orders.Commands;

/// <summary>Sets the restaurant's order-number prefix and next number, from the Restaurant Setup screen. Only affects orders created from now on - already-created orders keep their existing numbers.</summary>
public sealed record ConfigureOrderNumberSequenceCommand(string Prefix, int StartingNumber) : IRequest<OrderNumberSequenceDto>;

/// <summary>Handles <see cref="ConfigureOrderNumberSequenceCommand"/>.</summary>
public sealed class ConfigureOrderNumberSequenceCommandHandler(IOrderNumberSequenceRepository repository)
    : IRequestHandler<ConfigureOrderNumberSequenceCommand, OrderNumberSequenceDto>
{
    /// <inheritdoc/>
    public async Task<OrderNumberSequenceDto> Handle(ConfigureOrderNumberSequenceCommand request, CancellationToken cancellationToken)
    {
        var sequence = await repository.GetSingletonAsync(cancellationToken);
        if (sequence is null)
        {
            sequence = OrderNumberSequence.CreateDefault();
            await repository.AddAsync(sequence, cancellationToken);
        }

        sequence.Configure(request.Prefix, request.StartingNumber);

        return OrderNumberSequenceDto.FromDomain(sequence);
    }
}
