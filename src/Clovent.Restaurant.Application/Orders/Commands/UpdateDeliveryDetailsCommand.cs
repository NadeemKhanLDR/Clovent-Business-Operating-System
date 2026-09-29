using Clovent.Restaurant.Application.Orders.Dtos;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.ServiceCharges;
using MediatR;

namespace Clovent.Restaurant.Application.Orders.Commands;

/// <summary>Updates delivery details, address, notes, fee, or rider on an existing order.</summary>
public sealed record UpdateDeliveryDetailsCommand(
    Guid OrderId,
    OrderSource OrderSource,
    string? DeliveryCustomerName,
    string? DeliveryPhone,
    string? DeliveryAddress,
    string? DeliveryNotes,
    decimal DeliveryFee = 0m,
    string? RiderName = null,
    string? RiderPhone = null,
    DeliveryStatus? DeliveryStatus = null) : IRequest<OrderDto>;

/// <summary>Handles <see cref="UpdateDeliveryDetailsCommand"/>.</summary>
public sealed class UpdateDeliveryDetailsCommandHandler(
    IOrderRepository orderRepository,
    IServiceChargeRepository serviceChargeRepository)
    : IRequestHandler<UpdateDeliveryDetailsCommand, OrderDto>
{
    /// <inheritdoc/>
    public async Task<OrderDto> Handle(UpdateDeliveryDetailsCommand request, CancellationToken cancellationToken)
    {
        var orderId = new OrderId(request.OrderId);
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        var oldFee = order.DeliveryFee;
        order.SetDeliveryDetails(
            request.OrderSource,
            request.DeliveryCustomerName,
            request.DeliveryPhone,
            request.DeliveryAddress,
            request.DeliveryNotes,
            request.DeliveryFee,
            request.RiderName,
            request.RiderPhone);

        if (request.DeliveryStatus.HasValue && request.DeliveryStatus.Value != Clovent.Restaurant.Orders.DeliveryStatus.None)
        {
            order.UpdateDeliveryStatus(request.DeliveryStatus.Value);
        }

        // If delivery fee changed, reconcile service charge
        if (request.DeliveryFee != oldFee)
        {
            var existingCharges = await serviceChargeRepository.GetByOrderIdAsync(orderId, cancellationToken);
            var deliveryCharges = existingCharges.Where(sc => string.Equals(sc.Reason, "Delivery Fee", StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var ch in deliveryCharges)
            {
                order.RemoveServiceCharge(ch.Id);
            }

            if (request.DeliveryFee > 0)
            {
                var newCharge = ServiceCharge.Create(orderId, ServiceChargeType.FixedAmount, request.DeliveryFee, "Delivery Fee");
                await serviceChargeRepository.AddAsync(newCharge, cancellationToken);
                order.ApplyServiceCharge(newCharge.Id);
            }
        }

        return OrderDto.FromDomain(order);
    }
}
