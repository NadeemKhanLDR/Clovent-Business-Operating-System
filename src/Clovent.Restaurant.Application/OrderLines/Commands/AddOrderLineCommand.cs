using Clovent.Catalog.Application.Prices.Queries;
using Clovent.Catalog.Application.Products.Queries;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Restaurant.Application.OrderLines.Dtos;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using MediatR;

namespace Clovent.Restaurant.Application.OrderLines.Commands;

/// <summary>
/// Adds a line to an order. Resolves the variant's active selling price and
/// its product's tax configuration from <c>Clovent.Catalog.Application</c>'s
/// existing queries and snapshots both onto the new line (see
/// <see cref="OrderLine"/>'s doc comment for why) - consuming the
/// catalog module's read model rather than duplicating its pricing/tax
/// logic here.
/// </summary>
public sealed record AddOrderLineCommand(Guid OrderId, Guid ProductVariantId, decimal Quantity, string? Notes = null) : IRequest<OrderLineDto>;

/// <summary>Handles <see cref="AddOrderLineCommand"/>.</summary>
public sealed class AddOrderLineCommandHandler(IOrderRepository orderRepository, IOrderLineRepository orderLineRepository, IMediator mediator)
    : IRequestHandler<AddOrderLineCommand, OrderLineDto>
{
    /// <inheritdoc/>
    public async Task<OrderLineDto> Handle(AddOrderLineCommand request, CancellationToken cancellationToken)
    {
        var orderId = new OrderId(request.OrderId);
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        var variant = await mediator.Send(new GetProductVariantByIdQuery(request.ProductVariantId), cancellationToken);
        var product = await mediator.Send(new GetProductByIdQuery(variant.ProductId), cancellationToken);
        var prices = await mediator.Send(new ListProductPricesByVariantQuery(request.ProductVariantId), cancellationToken);

        var sellingPrice = prices
            .Where(p => p.Status == "Active" && p.PriceType == "Selling")
            .OrderByDescending(p => p.EffectiveFromUtc)
            .FirstOrDefault();

        var line = OrderLine.Create(
            orderId,
            new Clovent.Catalog.Variants.ProductVariantId(request.ProductVariantId),
            request.Quantity,
            sellingPrice?.Amount ?? 0m,
            product.TaxRatePercentage,
            product.TaxIsInclusive,
            request.Notes);

        order.AddOrderLine(line.Id);
        await orderLineRepository.AddAsync(line, cancellationToken);

        return OrderLineDto.FromDomain(line);
    }
}
