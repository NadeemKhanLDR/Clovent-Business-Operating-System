using Clovent.Catalog.Application.Prices.Queries;
using Clovent.Catalog.Application.Products.Queries;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Restaurant.Application.CustomerReorder.Dtos;
using Clovent.Restaurant.Customers;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using MediatR;

namespace Clovent.Restaurant.Application.CustomerReorder.Queries;

/// <summary>
/// Retrieves a customer's most recent completed order with every line
/// resolved against the current catalog: names, today's selling prices, and
/// an <c>IsAvailable</c> flag that is true only when both the variant and
/// its product are still active. The reorder form replays the lines through
/// the ordinary <c>AddOrderLineCommand</c> - no separate build command.
/// </summary>
public sealed record GetCustomerLastOrderQuery(Guid CustomerId) : IRequest<CustomerReorderDto?>;

/// <summary>Handles <see cref="GetCustomerLastOrderQuery"/>.</summary>
public sealed class GetCustomerLastOrderQueryHandler(
    ICustomerRepository customerRepository,
    IOrderRepository orderRepository,
    IOrderLineRepository orderLineRepository,
    IMediator mediator) : IRequestHandler<GetCustomerLastOrderQuery, CustomerReorderDto?>
{
    /// <inheritdoc/>
    public async Task<CustomerReorderDto?> Handle(GetCustomerLastOrderQuery request, CancellationToken cancellationToken)
    {
        var customerId = new CustomerId(request.CustomerId);
        var customer = await customerRepository.GetByIdAsync(customerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), request.CustomerId);

        var lastOrder = (await orderRepository.GetAllAsync(cancellationToken))
            .Where(o => o.CustomerId == customerId && o.Status == OrderStatus.Completed)
            .OrderByDescending(o => o.UpdatedAtUtc)
            .FirstOrDefault();

        if (lastOrder is null)
        {
            return null;
        }

        var catalog = await CatalogSnapshot.LoadAsync(mediator, cancellationToken);
        var lines = await orderLineRepository.GetByOrderIdAsync(lastOrder.Id, cancellationToken);

        return new CustomerReorderDto(
            lastOrder.Id.Value,
            lastOrder.OrderNumber.Value,
            lastOrder.UpdatedAtUtc,
            [.. lines.Where(l => !l.IsVoided).Select(catalog.ProjectLine)]);
    }
}

/// <summary>
/// The shared one-shot view of the catalog read model (variants, products,
/// newest active selling price per variant) both CustomerReorder queries
/// resolve <see cref="CustomerReorderLineDto"/>s against.
/// </summary>
internal sealed class CatalogSnapshot
{
    public static async Task<CatalogSnapshot> LoadAsync(IMediator mediator, CancellationToken cancellationToken)
    {
        var variants = await mediator.Send(new ListProductVariantsQuery(), cancellationToken);
        var products = await mediator.Send(new ListProductsQuery(), cancellationToken);
        var sellingPrices = await mediator.Send(new ListActiveProductPricesByTypeQuery(Clovent.Catalog.Prices.PriceType.Selling), cancellationToken);

        return new CatalogSnapshot(
            variants.ToDictionary(v => v.ProductVariantId),
            products.ToDictionary(p => p.ProductId),
            sellingPrices
                .GroupBy(p => p.ProductVariantId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.EffectiveFromUtc).First().Amount));
    }

    private readonly Dictionary<Guid, Clovent.Catalog.Application.Variants.Dtos.ProductVariantDto> _variantById;
    private readonly Dictionary<Guid, Clovent.Catalog.Application.Products.Dtos.ProductDto> _productById;
    private readonly Dictionary<Guid, decimal> _unitPriceByVariantId;

    private CatalogSnapshot(
        Dictionary<Guid, Clovent.Catalog.Application.Variants.Dtos.ProductVariantDto> variantById,
        Dictionary<Guid, Clovent.Catalog.Application.Products.Dtos.ProductDto> productById,
        Dictionary<Guid, decimal> unitPriceByVariantId)
    {
        _variantById = variantById;
        _productById = productById;
        _unitPriceByVariantId = unitPriceByVariantId;
    }

    /// <summary>Projects an order line into a reorder line, resolving availability and current names/prices.</summary>
    public CustomerReorderLineDto ProjectLine(OrderLine line)
    {
        var variantId = line.ProductVariantId.Value;
        var variant = _variantById.GetValueOrDefault(variantId);
        var product = variant is not null ? _productById.GetValueOrDefault(variant.ProductId) : null;

        var isAvailable = variant is not null
            && variant.Status == "Active"
            && (variant.ProductStatus is null || variant.ProductStatus == "Active");

        return new CustomerReorderLineDto(
            variantId,
            product?.Name ?? "(unknown product)",
            variant?.Name ?? "(unknown variant)",
            line.Quantity,
            _unitPriceByVariantId.GetValueOrDefault(variantId, line.UnitPrice),
            isAvailable);
    }

    /// <summary>Projects a bare variant id + quantity into a reorder line - the frequency view has no single source order line to snapshot.</summary>
    public CustomerReorderLineDto ProjectSnapshot(Guid variantId, decimal quantity)
    {
        var variant = _variantById.GetValueOrDefault(variantId);
        var product = variant is not null ? _productById.GetValueOrDefault(variant.ProductId) : null;

        var isAvailable = variant is not null
            && variant.Status == "Active"
            && (variant.ProductStatus is null || variant.ProductStatus == "Active");

        return new CustomerReorderLineDto(
            variantId,
            product?.Name ?? "(unknown product)",
            variant?.Name ?? "(unknown variant)",
            quantity,
            _unitPriceByVariantId.GetValueOrDefault(variantId, 0m),
            isAvailable);
    }
}
