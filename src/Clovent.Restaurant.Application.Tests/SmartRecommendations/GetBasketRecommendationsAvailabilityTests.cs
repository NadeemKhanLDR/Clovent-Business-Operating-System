using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Catalog.Variants;
using Clovent.Restaurant.Application.SmartRecommendations.Queries;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.SmartRecommendations;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.SmartRecommendations;

/// <summary>
/// Availability rules for the suggestion engine: an inactive recommended
/// product or variant must never be suggested - the cashier could not sell it.
/// </summary>
public class GetBasketRecommendationsAvailabilityTests
{
    private static readonly Guid TriggerProductId = Guid.NewGuid();
    private static readonly Guid DeadProductProductId = Guid.NewGuid();
    private static readonly Guid DeadVariantProductId = Guid.NewGuid();
    private static readonly Guid LiveProductId = Guid.NewGuid();

    private readonly Guid _triggerVariant = Guid.NewGuid();
    private readonly Guid _deadProductVariant = Guid.NewGuid();
    private readonly Guid _deadVariant = Guid.NewGuid();
    private readonly Guid _liveVariant = Guid.NewGuid();

    private readonly FakeRecommendationRuleRepository _ruleRepository = new();
    private readonly FakeOrderRepository _orderRepository = new();
    private readonly FakeOrderLineRepository _orderLineRepository = new();

    private GetBasketRecommendationsQueryHandler CreateHandler() =>
        new(
            _ruleRepository,
            _orderRepository,
            _orderLineRepository,
            CatalogFakes.Mediator(
                new[]
                {
                    CatalogFakes.Variant(_triggerVariant, TriggerProductId, "Trigger"),
                    CatalogFakes.Variant(_deadProductVariant, DeadProductProductId, "Dead Product", productStatus: "Inactive"),
                    CatalogFakes.Variant(_deadVariant, DeadVariantProductId, "Dead Variant", status: "Inactive"),
                    CatalogFakes.Variant(_liveVariant, LiveProductId, "Live"),
                },
                new[]
                {
                    CatalogFakes.Product(TriggerProductId, "Trigger"),
                    CatalogFakes.Product(DeadProductProductId, "Dead Product"),
                    CatalogFakes.Product(DeadVariantProductId, "Dead Variant"),
                    CatalogFakes.Product(LiveProductId, "Live"),
                },
                new[]
                {
                    CatalogFakes.SellingPrice(_deadProductVariant, 100m),
                    CatalogFakes.SellingPrice(_deadVariant, 100m),
                    CatalogFakes.SellingPrice(_liveVariant, 80m),
                }));

    [Fact]
    public async Task InactiveRecommendedProduct_IsNeverSuggested()
    {
        _ruleRepository.Add(RecommendationRule.Create(null, new ProductVariantId(_deadProductVariant), priority: 1));

        var results = await CreateHandler().Handle(
            new GetBasketRecommendationsQuery(null, [_triggerVariant], DateTime.Today.AddHours(12), 3),
            CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task InactiveRecommendedVariant_IsNeverSuggested()
    {
        _ruleRepository.Add(RecommendationRule.Create(null, new ProductVariantId(_deadVariant), priority: 1));

        var results = await CreateHandler().Handle(
            new GetBasketRecommendationsQuery(null, [_triggerVariant], DateTime.Today.AddHours(12), 3),
            CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task ActiveRecommendedVariant_IsStillSuggested()
    {
        _ruleRepository.Add(RecommendationRule.Create(null, new ProductVariantId(_liveVariant), priority: 1));

        var results = await CreateHandler().Handle(
            new GetBasketRecommendationsQuery(null, [_triggerVariant], DateTime.Today.AddHours(12), 3),
            CancellationToken.None);

        Assert.Equal([_liveVariant], [.. results.Select(r => r.VariantId)]);
    }
}
