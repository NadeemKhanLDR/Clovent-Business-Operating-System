using Clovent.Restaurant.Application.ServiceCharges.Dtos;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.ServiceCharges;
using MediatR;

namespace Clovent.Restaurant.Application.ServiceCharges.Commands;

/// <summary>Removes a service charge from an order. The charge record itself is kept for history, just detached from the order's tracked list.</summary>
public sealed record RemoveServiceChargeFromOrderCommand(Guid OrderId, Guid ServiceChargeId) : IRequest<ServiceChargeDto>;

/// <summary>Handles <see cref="RemoveServiceChargeFromOrderCommand"/>.</summary>
public sealed class RemoveServiceChargeFromOrderCommandHandler(IOrderRepository orderRepository, IServiceChargeRepository serviceChargeRepository)
    : IRequestHandler<RemoveServiceChargeFromOrderCommand, ServiceChargeDto>
{
    /// <inheritdoc/>
    public async Task<ServiceChargeDto> Handle(RemoveServiceChargeFromOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(new OrderId(request.OrderId), cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        var serviceChargeId = new ServiceChargeId(request.ServiceChargeId);
        var charge = await serviceChargeRepository.GetByIdAsync(serviceChargeId, cancellationToken)
            ?? throw new NotFoundException(nameof(ServiceCharge), request.ServiceChargeId);

        order.RemoveServiceCharge(serviceChargeId);

        return ServiceChargeDto.FromDomain(charge);
    }
}
