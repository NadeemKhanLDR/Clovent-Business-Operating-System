using Clovent.Restaurant.Application.ServiceCharges.Dtos;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.ServiceCharges;
using MediatR;

namespace Clovent.Restaurant.Application.ServiceCharges.Commands;

/// <summary>Creates a service charge and applies it to an order.</summary>
public sealed record ApplyServiceChargeToOrderCommand(Guid OrderId, ServiceChargeType ServiceChargeType, decimal Value, string Reason) : IRequest<ServiceChargeDto>;

/// <summary>Handles <see cref="ApplyServiceChargeToOrderCommand"/>.</summary>
public sealed class ApplyServiceChargeToOrderCommandHandler(IOrderRepository orderRepository, IServiceChargeRepository serviceChargeRepository)
    : IRequestHandler<ApplyServiceChargeToOrderCommand, ServiceChargeDto>
{
    /// <inheritdoc/>
    public async Task<ServiceChargeDto> Handle(ApplyServiceChargeToOrderCommand request, CancellationToken cancellationToken)
    {
        var orderId = new OrderId(request.OrderId);
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        var charge = ServiceCharge.Create(orderId, request.ServiceChargeType, request.Value, request.Reason);
        order.ApplyServiceCharge(charge.Id);

        await serviceChargeRepository.AddAsync(charge, cancellationToken);

        return ServiceChargeDto.FromDomain(charge);
    }
}
