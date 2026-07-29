using Clovent.Restaurant.Application.Discounts.Dtos;
using Clovent.Restaurant.Discounts;
using Clovent.Restaurant.Orders;
using MediatR;

namespace Clovent.Restaurant.Application.Discounts.Commands;

/// <summary>Removes a discount from an order. The discount record itself is kept for history, just detached from the order's tracked list.</summary>
public sealed record RemoveDiscountFromOrderCommand(Guid OrderId, Guid DiscountId) : IRequest<DiscountDto>;

/// <summary>Handles <see cref="RemoveDiscountFromOrderCommand"/>.</summary>
public sealed class RemoveDiscountFromOrderCommandHandler(IOrderRepository orderRepository, IDiscountRepository discountRepository)
    : IRequestHandler<RemoveDiscountFromOrderCommand, DiscountDto>
{
    /// <inheritdoc/>
    public async Task<DiscountDto> Handle(RemoveDiscountFromOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(new OrderId(request.OrderId), cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        var discountId = new DiscountId(request.DiscountId);
        var discount = await discountRepository.GetByIdAsync(discountId, cancellationToken)
            ?? throw new NotFoundException(nameof(Discount), request.DiscountId);

        order.RemoveDiscount(discountId);

        return DiscountDto.FromDomain(discount);
    }
}
