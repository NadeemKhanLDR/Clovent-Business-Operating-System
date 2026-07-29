using Clovent.Restaurant.Application.Discounts.Dtos;
using Clovent.Restaurant.Discounts;
using Clovent.Restaurant.Orders;
using MediatR;

namespace Clovent.Restaurant.Application.Discounts.Commands;

/// <summary>Creates a discount and applies it to an order.</summary>
public sealed record ApplyDiscountToOrderCommand(Guid OrderId, DiscountType DiscountType, decimal Value, string Reason) : IRequest<DiscountDto>;

/// <summary>Handles <see cref="ApplyDiscountToOrderCommand"/>.</summary>
public sealed class ApplyDiscountToOrderCommandHandler(IOrderRepository orderRepository, IDiscountRepository discountRepository)
    : IRequestHandler<ApplyDiscountToOrderCommand, DiscountDto>
{
    /// <inheritdoc/>
    public async Task<DiscountDto> Handle(ApplyDiscountToOrderCommand request, CancellationToken cancellationToken)
    {
        var orderId = new OrderId(request.OrderId);
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        var discount = Discount.Create(orderId, request.DiscountType, request.Value, request.Reason);
        order.ApplyDiscount(discount.Id);

        await discountRepository.AddAsync(discount, cancellationToken);

        return DiscountDto.FromDomain(discount);
    }
}
