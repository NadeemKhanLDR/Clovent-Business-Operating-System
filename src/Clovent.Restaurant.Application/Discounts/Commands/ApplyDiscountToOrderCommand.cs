using Clovent.Restaurant.Application.Discounts.Dtos;
using Clovent.Restaurant.Application.OrderLines.Dtos;
using Clovent.Restaurant.Application.Orders;
using Clovent.Restaurant.Discounts;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using MediatR;

namespace Clovent.Restaurant.Application.Discounts.Commands;

/// <summary>Creates a discount and applies it to an order.</summary>
public sealed record ApplyDiscountToOrderCommand(Guid OrderId, DiscountType DiscountType, decimal Value, string Reason) : IRequest<DiscountDto>;

/// <summary>Handles <see cref="ApplyDiscountToOrderCommand"/>.</summary>
public sealed class ApplyDiscountToOrderCommandHandler(
    IOrderRepository orderRepository,
    IDiscountRepository discountRepository,
    IOrderLineRepository orderLineRepository)
    : IRequestHandler<ApplyDiscountToOrderCommand, DiscountDto>
{
    /// <summary>Half-cent tolerance, matching the slack every other money comparison in this module applies.</summary>
    private const decimal MoneyEpsilon = 0.005m;

    /// <inheritdoc/>
    public async Task<DiscountDto> Handle(ApplyDiscountToOrderCommand request, CancellationToken cancellationToken)
    {
        var orderId = new OrderId(request.OrderId);
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        // Intrinsic validity first (non-negative, percentage <= 100) - that is
        // all Discount.Create can judge on its own. Nothing is persisted until
        // the order-relative check below also passes.
        var discount = Discount.Create(orderId, request.DiscountType, request.Value, request.Reason);

        await RequireDiscountFitsOrderAsync(orderId, discount, cancellationToken);

        order.ApplyDiscount(discount.Id);

        await discountRepository.AddAsync(discount, cancellationToken);

        return DiscountDto.FromDomain(discount);
    }

    /// <summary>
    /// Rejects a discount that would take the order's discounts past what the
    /// goods on it are actually worth.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Root cause this closes (M-5):</b> <see cref="Discount.Create"/>
    /// rejects a negative value and a percentage above 100, but a
    /// <see cref="DiscountType.FixedAmount"/> had no upper bound at all - it
    /// cannot have one, since the domain object has no idea what the order is
    /// worth. A 500.00 fixed discount on a 50.00 bill therefore persisted
    /// happily and drove <see cref="OrderTotals.GrandTotal"/> negative, which
    /// in turn produced a negative balance that
    /// <c>CompleteOrderCommandHandler</c> then accepted (M-4).
    /// </para>
    /// <para>
    /// <b>Why the check lives here:</b> it needs the order's lines, which is an
    /// application-layer concern - the same reason the balance check sits in
    /// <c>CompleteOrderCommandHandler</c> rather than in
    /// <see cref="Order.Complete"/>. It reuses
    /// <see cref="OrderTotalsCalculator"/> rather than re-deriving how a
    /// percentage and a fixed amount each resolve, so the ceiling can never
    /// drift from the arithmetic the rest of the POS displays.
    /// </para>
    /// <para>
    /// <b>Cumulative, not per-discount:</b> an order may carry several
    /// discounts, so the test is the whole set - existing plus proposed -
    /// against the subtotal. Checking the new one alone would let two 30.00
    /// discounts settle on a 50.00 bill. Percentage discounts are measured by
    /// the same rule and are unaffected in practice: a single one is capped at
    /// 100% by the domain, and 100% resolves to exactly the subtotal, which is
    /// allowed. Service charges and tax are deliberately left out of the
    /// comparison - they are additions, so keeping discounts at or below the
    /// subtotal guarantees a non-negative grand total without letting a service
    /// charge quietly fund a larger discount.
    /// </para>
    /// </remarks>
    /// <exception cref="RestaurantDomainException">The resulting discounts would exceed the order's subtotal.</exception>
    private async Task RequireDiscountFitsOrderAsync(OrderId orderId, Discount proposed, CancellationToken cancellationToken)
    {
        var lines = await orderLineRepository.GetByOrderIdAsync(orderId, cancellationToken);
        var existing = await discountRepository.GetByOrderIdAsync(orderId, cancellationToken);

        var discountDtos = existing.Select(DiscountDto.FromDomain).ToList();
        discountDtos.Add(DiscountDto.FromDomain(proposed));

        var totals = OrderTotalsCalculator.Calculate(
            lines.Select(OrderLineDto.FromDomain).ToList(),
            discountDtos,
            [],
            []);

        if (totals.DiscountTotal > totals.Subtotal + MoneyEpsilon)
        {
            throw RestaurantDomainException.DiscountExceedsOrderSubtotal(orderId, totals.DiscountTotal, totals.Subtotal);
        }
    }
}
