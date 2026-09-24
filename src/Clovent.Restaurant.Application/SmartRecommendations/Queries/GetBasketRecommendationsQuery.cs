using Clovent.Catalog.Application.Prices.Queries;
using Clovent.Catalog.Application.Products.Queries;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Restaurant.Application.SmartRecommendations.Dtos;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.SmartRecommendations;
using MediatR;

namespace Clovent.Restaurant.Application.SmartRecommendations.Queries;

/// <summary>
/// Retrieves up to <see cref="Take"/> deterministic suggestions for the
/// current basket: active <see cref="RecommendationRule"/>s whose trigger
/// product (or "any basket") matches, whose local time window and day mask
/// contain <see cref="LocalTime"/>, and whose variant is not already in the
/// basket - ordered by rule priority - then, if fewer than
/// <see cref="Take"/> matched, fills the remaining slots with today's
/// best-selling variants ("popular today", aggregated from today's completed
/// order lines) that are also absent from the basket. Product/variant names
/// and current selling prices come from <c>Clovent.Catalog.Application</c>'s
/// existing queries - the same read model the POS product wall uses.
/// </summary>
public sealed record GetBasketRecommendationsQuery(
    Guid? CustomerId,
    IReadOnlyCollection<Guid> BasketVariantIds,
    DateTime LocalTime,
    int Take = 3) : IRequest<IReadOnlyCollection<BasketRecommendationDto>>;

/// <summary>Handles <see cref="GetBasketRecommendationsQuery"/>.</summary>
public sealed class GetBasketRecommendationsQueryHandler(
    IRecommendationRuleRepository ruleRepository,
    IOrderRepository orderRepository,
    IOrderLineRepository orderLineRepository,
    IMediator mediator) : IRequestHandler<GetBasketRecommendationsQuery, IReadOnlyCollection<BasketRecommendationDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<BasketRecommendationDto>> Handle(GetBasketRecommendationsQuery request, CancellationToken cancellationToken)
    {
        if (request.Take <= 0)
        {
            return [];
        }

        var basketVariantIds = request.BasketVariantIds.Distinct().ToHashSet();

        // The same catalog read model the POS product wall resolves names/prices through.
        var variants = await mediator.Send(new ListProductVariantsQuery(), cancellationToken);
        var products = await mediator.Send(new ListProductsQuery(), cancellationToken);
        var sellingPrices = await mediator.Send(new ListActiveProductPricesByTypeQuery(Clovent.Catalog.Prices.PriceType.Selling), cancellationToken);

        var variantById = variants
            .Where(v => string.Equals(v.Status, "Active", StringComparison.OrdinalIgnoreCase)
                && string.Equals(v.ProductStatus ?? "Active", "Active", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(v => v.ProductVariantId);
        var productNameByProductId = products
            .Where(p => string.Equals(p.Status, "Active", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(p => p.ProductId, p => p.Name);
        var unitPriceByVariantId = sellingPrices
            .GroupBy(p => p.ProductVariantId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.EffectiveFromUtc).First().Amount);

        string ProductName(Guid variantId) =>
            variantById.TryGetValue(variantId, out var variant) && productNameByProductId.TryGetValue(variant.ProductId, out var name)
                ? name
                : "(unknown product)";
        string VariantName(Guid variantId) =>
            variantById.TryGetValue(variantId, out var variant) ? variant.Name : "(unknown variant)";

        var results = new List<BasketRecommendationDto>();
        var suggestedVariantIds = new HashSet<Guid>();

        // 1. Configured rules, in priority order.
        var localTimeOfDay = request.LocalTime.TimeOfDay;
        var basketProductIds = basketVariantIds
            .Where(variantById.ContainsKey)
            .Select(id => variantById[id].ProductId)
            .ToList();

        var activeRules = await ruleRepository.GetActiveAsync(cancellationToken);
        foreach (var rule in activeRules.OrderBy(r => r.Priority))
        {
            if (results.Count >= request.Take)
            {
                break;
            }

            var variantId = rule.RecommendedVariantId.Value;
            if (basketVariantIds.Contains(variantId) || suggestedVariantIds.Contains(variantId))
            {
                continue;
            }

            // Never suggest an inactive product or inactive variant - the cashier
            // could not actually sell it (CatalogStatus on both DTOs).
            if (!variantById.ContainsKey(variantId))
            {
                continue;
            }

            if (!rule.Matches(basketProductIds, localTimeOfDay, request.LocalTime.DayOfWeek))
            {
                continue;
            }

            suggestedVariantIds.Add(variantId);
            results.Add(new BasketRecommendationDto(
                variantId,
                ProductName(variantId),
                VariantName(variantId),
                unitPriceByVariantId.GetValueOrDefault(variantId, 0m),
                RecommendationReason.ConfiguredRule));
        }

        // 2. "Popular today" fallback - today's completed order lines aggregated by variant.
        if (results.Count < request.Take)
        {
            var today = DateOnly.FromDateTime(request.LocalTime.Date);
            var quantitiesByVariantId = new Dictionary<Guid, decimal>();

            var completedToday = (await orderRepository.GetAllAsync(cancellationToken))
                .Where(o => o.Status == OrderStatus.Completed && DateOnly.FromDateTime(o.UpdatedAtUtc.UtcDateTime) == today);
            foreach (var order in completedToday)
            {
                var lines = await orderLineRepository.GetByOrderIdAsync(order.Id, cancellationToken);
                foreach (var line in lines.Where(l => !l.IsVoided))
                {
                    quantitiesByVariantId[line.ProductVariantId.Value] =
                        quantitiesByVariantId.GetValueOrDefault(line.ProductVariantId.Value) + line.Quantity;
                }
            }

            foreach (var (variantId, _) in quantitiesByVariantId
                         .Where(kv => !basketVariantIds.Contains(kv.Key) && !suggestedVariantIds.Contains(kv.Key))
                         .OrderByDescending(kv => kv.Value)
                         .ThenBy(kv => kv.Key))
            {
                if (results.Count >= request.Take)
                {
                    break;
                }

                if (!suggestedVariantIds.Add(variantId) || !variantById.ContainsKey(variantId))
                {
                    continue;
                }

                results.Add(new BasketRecommendationDto(
                    variantId,
                    ProductName(variantId),
                    VariantName(variantId),
                    unitPriceByVariantId.GetValueOrDefault(variantId, 0m),
                    RecommendationReason.PopularToday));
            }
        }

        return results;
    }
}
