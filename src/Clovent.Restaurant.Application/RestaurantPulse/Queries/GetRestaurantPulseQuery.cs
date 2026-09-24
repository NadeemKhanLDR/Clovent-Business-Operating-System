using Clovent.Catalog.Application.Categories.Queries;
using Clovent.Catalog.Application.Products.Queries;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Restaurant.Application.RestaurantPulse.Dtos;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using MediatR;

namespace Clovent.Restaurant.Application.RestaurantPulse.Queries;

/// <summary>
/// Computes the Restaurant Pulse panel for the given date (defaults to
/// today, UTC). The handler gathers only the two relevant days' completed
/// orders (today + yesterday for the comparison figure), aggregates them
/// into <see cref="RestaurantPulseOrderSample"/>s, and delegates every
/// figure to the pure <see cref="RestaurantPulseCalculator"/>. Inventory is
/// flagged unavailable: low-stock data lives in the Inventory module, not
/// Restaurant, so <see cref="RestaurantPulseDto.InventoryAvailable"/> is
/// false and <see cref="RestaurantPulseDto.LowStockItems"/> empty until
/// that changes.
/// </summary>
public sealed record GetRestaurantPulseQuery(DateOnly? TodayUtcDate = null) : IRequest<RestaurantPulseDto>;

/// <summary>Handles <see cref="GetRestaurantPulseQuery"/>.</summary>
public sealed class GetRestaurantPulseQueryHandler(
    IOrderRepository orderRepository,
    IOrderLineRepository orderLineRepository,
    IMediator mediator) : IRequestHandler<GetRestaurantPulseQuery, RestaurantPulseDto>
{
    private const string BeverageNameFragment1 = "drink";
    private const string BeverageNameFragment2 = "beverage";

    /// <inheritdoc/>
    public async Task<RestaurantPulseDto> Handle(GetRestaurantPulseQuery request, CancellationToken cancellationToken)
    {
        var today = request.TodayUtcDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);

        var relevantOrders = (await orderRepository.GetAllAsync(cancellationToken))
            .Where(o => o.Status == OrderStatus.Completed)
            .Where(o =>
            {
                var date = DateOnly.FromDateTime(o.UpdatedAtUtc.UtcDateTime);
                return date == today || date == yesterday;
            })
            .ToList();

        // Catalog read model: names + which variants belong to a drink/beverage category.
        var variants = await mediator.Send(new ListProductVariantsQuery(), cancellationToken);
        var products = await mediator.Send(new ListProductsQuery(), cancellationToken);
        var categories = await mediator.Send(new ListProductCategoriesQuery(), cancellationToken);

        var productById = products.ToDictionary(p => p.ProductId);
        var variantById = variants.ToDictionary(v => v.ProductVariantId);
        var beverageCategoryIds = categories
            .Where(c => c.Name.Contains(BeverageNameFragment1, StringComparison.OrdinalIgnoreCase)
                || c.Name.Contains(BeverageNameFragment2, StringComparison.OrdinalIgnoreCase))
            .Select(c => c.ProductCategoryId)
            .ToHashSet();
        var isBeverageByVariantId = variants
            .Where(v => productById.TryGetValue(v.ProductId, out var product)
                && product.CategoryId is not null
                && beverageCategoryIds.Contains(product.CategoryId.Value))
            .ToDictionary(v => v.ProductVariantId, _ => true);
        var beverageInfoAvailable = categories.Count > 0;

        string ProductName(Clovent.Catalog.Variants.ProductVariantId variantId) =>
            variantById.TryGetValue(variantId.Value, out var variant) && productById.TryGetValue(variant.ProductId, out var product)
                ? product.Name
                : "(unknown product)";

        var yesterdaySales = 0m;
        var todaySamples = new List<RestaurantPulseOrderSample>();
        var todayBeverageUnitPrices = new List<decimal>();

        foreach (var order in relevantOrders)
        {
            var lines = (await orderLineRepository.GetByOrderIdAsync(order.Id, cancellationToken))
                .Where(l => !l.IsVoided)
                .ToList();

            var sampleLines = lines
                .Select(l => new RestaurantPulseLineSample(l.ProductVariantId.Value, ProductName(l.ProductVariantId), l.Quantity, l.LineTotal))
                .ToList();

            if (DateOnly.FromDateTime(order.UpdatedAtUtc.UtcDateTime) == yesterday)
            {
                yesterdaySales += sampleLines.Sum(l => l.LineTotal);
                continue;
            }

            var hasBeverage = lines.Any(l => isBeverageByVariantId.ContainsKey(l.ProductVariantId.Value));
            todayBeverageUnitPrices.AddRange(lines
                .Where(l => isBeverageByVariantId.ContainsKey(l.ProductVariantId.Value))
                .Select(l => l.UnitPrice));

            todaySamples.Add(new RestaurantPulseOrderSample(
                order.CreatedAtUtc,
                order.UpdatedAtUtc,
                sampleLines.Sum(l => l.LineTotal),
                sampleLines,
                hasBeverage));
        }

        return RestaurantPulseCalculator.Compute(todaySamples, yesterdaySales, todayBeverageUnitPrices, beverageInfoAvailable);
    }
}
