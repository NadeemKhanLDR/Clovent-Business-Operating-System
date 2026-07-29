using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Orders;
using MediatR;

namespace Clovent.Restaurant.Application.Orders.Commands;

/// <summary>Resumes a held order.</summary>
public sealed record ResumeOrderCommand(Guid OrderId) : IRequest<OrderDto>;

/// <summary>Handles <see cref="ResumeOrderCommand"/>.</summary>
public sealed class ResumeOrderCommandHandler(IOrderRepository repository) : IRequestHandler<ResumeOrderCommand, OrderDto>
{
    /// <inheritdoc/>
    public async Task<OrderDto> Handle(ResumeOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await repository.GetByIdAsync(new OrderId(request.OrderId), cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        order.Resume();
        return OrderDto.FromDomain(order);
    }
}
