using Clovent.Restaurant.Application.OrderLines.Dtos;
using Clovent.Restaurant.OrderLines;
using MediatR;

namespace Clovent.Restaurant.Application.OrderLines.Commands;

/// <summary>Sets an order line's item notes.</summary>
public sealed record SetOrderLineNotesCommand(Guid OrderLineId, string? Notes) : IRequest<OrderLineDto>;

/// <summary>Handles <see cref="SetOrderLineNotesCommand"/>.</summary>
public sealed class SetOrderLineNotesCommandHandler(IOrderLineRepository repository) : IRequestHandler<SetOrderLineNotesCommand, OrderLineDto>
{
    /// <inheritdoc/>
    public async Task<OrderLineDto> Handle(SetOrderLineNotesCommand request, CancellationToken cancellationToken)
    {
        var line = await repository.GetByIdAsync(new OrderLineId(request.OrderLineId), cancellationToken)
            ?? throw new NotFoundException(nameof(OrderLine), request.OrderLineId);

        line.SetNotes(request.Notes);
        return OrderLineDto.FromDomain(line);
    }
}
