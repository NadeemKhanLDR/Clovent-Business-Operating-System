using Clovent.Catalog.Application.Products.Queries;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Restaurant.Application.SmartRecommendations.Dtos;
using Clovent.Restaurant.SmartRecommendations;
using MediatR;

namespace Clovent.Restaurant.Application.SmartRecommendations.Queries;

/// <summary>
/// Retrieves the upsell performance grid for the Back Office: for each
/// recommended variant that had any analytics activity in the (inclusive)
/// UTC interval - offers, acceptances, dismissals, conversion rate, and
/// attributed upsell revenue. Revenue sums the quantity/amount snapshotted
/// on <see cref="SuggestionEventKind.Accepted"/> events only, so items the
/// cashier added from the normal menu are never counted as upsell revenue.
/// </summary>
public sealed record GetUpsellPerformanceQuery(
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc) : IRequest<IReadOnlyCollection<UpsellPerformanceDto>>;

/// <summary>Handles <see cref="GetUpsellPerformanceQuery"/>.</summary>
public sealed class GetUpsellPerformanceQueryHandler(
    ISuggestionEventRepository eventRepository,
    IMediator mediator) : IRequestHandler<GetUpsellPerformanceQuery, IReadOnlyCollection<UpsellPerformanceDto>>
{
    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<UpsellPerformanceDto>> Handle(GetUpsellPerformanceQuery request, CancellationToken cancellationToken)
    {
        var events = await eventRepository.GetRangeAsync(request.FromUtc, request.ToUtc, cancellationToken);
        if (events.Count == 0)
        {
            return [];
        }

        // Same catalog read model the POS uses - names are never stored on events.
        var variants = await mediator.Send(new ListProductVariantsQuery(), cancellationToken);
        var products = await mediator.Send(new ListProductsQuery(), cancellationToken);
        var productNameByProductId = products.ToDictionary(p => p.ProductId, p => p.Name);
        var variantById = variants.ToDictionary(v => v.ProductVariantId);

        return [..
            events
                .GroupBy(e => e.VariantId.Value)
                .Select(group =>
                {
                    var variantId = group.Key;
                    var offers = group.Count(e => e.Kind == SuggestionEventKind.Offered);
                    var accepted = group.Count(e => e.Kind == SuggestionEventKind.Accepted);
                    var dismissed = group.Count(e => e.Kind == SuggestionEventKind.Dismissed);
                    var revenue = group
                        .Where(e => e.Kind == SuggestionEventKind.Accepted)
                        .Sum(e => e.AcceptedQuantity * e.AcceptedUnitAmount);

                    variantById.TryGetValue(variantId, out var variant);
                    var productName = variant is not null && productNameByProductId.TryGetValue(variant.ProductId, out var name)
                        ? name
                        : "(unknown product)";

                    return new UpsellPerformanceDto(
                        variantId,
                        productName,
                        variant?.Name ?? "(unknown variant)",
                        offers,
                        accepted,
                        dismissed,
                        offers == 0 ? 0m : Math.Round(100m * accepted / offers, 1),
                        revenue);
                })
                .OrderByDescending(row => row.Accepted)
                .ThenBy(row => row.ProductName)
                .ThenBy(row => row.VariantName)];
    }
}
