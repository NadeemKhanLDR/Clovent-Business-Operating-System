using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Catalog.Variants;
using Clovent.MasterData.Warehouses;
using Clovent.Restaurant.Application.SmartRecommendations.Dtos;
using Clovent.Restaurant.Application.SmartRecommendations.Queries;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.SmartRecommendations;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.SmartRecommendations;

public class GetBasketRecommendationsQueryHandlerTests
{
    private static readonly Guid BurgerProductId = Guid.NewGuid();
    private static readonly Guid FriesProductId = Guid.NewGuid();
    private static readonly Guid ColaProductId = Guid.NewGuid();

    private readonly Guid _burgerVariantId = Guid.NewGuid();
    private readonly Guid _friesVariantId = Guid.NewGuid();
    private readonly Guid _colaVariantId = Guid.NewGuid();
    private readonly Guid _shakeVariantId = Guid.NewGuid();

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
                    CatalogFakes.Variant(_burgerVariantId, BurgerProductId, "Burger"),
                    CatalogFakes.Variant(_friesVariantId, FriesProductId, "Fries"),
                    CatalogFakes.Variant(_colaVariantId, ColaProductId, "Cola"),
                    CatalogFakes.Variant(_shakeVariantId, Guid.NewGuid(), "Shake"),
                },
                new[]
                {
                    CatalogFakes.Product(BurgerProductId, "Burger"),
                    CatalogFakes.Product(FriesProductId, "Fries"),
                    CatalogFakes.Product(ColaProductId, "Cola"),
                },
                new[]
                {
                    CatalogFakes.SellingPrice(_burgerVariantId, 500m),
                    CatalogFakes.SellingPrice(_friesVariantId, 150m),
                    CatalogFakes.SellingPrice(_colaVariantId, 80m),
                    CatalogFakes.SellingPrice(_shakeVariantId, 200m),
                }));

    [Fact]
    public async Task MatchingRules_ReturnedInPriorityOrder()
    {
        _ruleRepository.Add(RecommendationRule.Create(BurgerProductId, new ProductVariantId(_colaVariantId), priority: 2));
        _ruleRepository.Add(RecommendationRule.Create(null, new ProductVariantId(_friesVariantId), priority: 1)); // "any basket"

        var results = await CreateHandler().Handle(
            new GetBasketRecommendationsQuery(null, [_burgerVariantId], DateTime.Today.AddHours(12), 3),
            CancellationToken.None);

        Assert.Equal([_friesVariantId, _colaVariantId], [.. results.Select(r => r.VariantId)]);
        Assert.All(results, r => Assert.Equal(RecommendationReason.ConfiguredRule, r.Reason));
        Assert.Equal(150m, results.ToList()[0].UnitPrice);
        Assert.Equal("Fries", results.ToList()[0].VariantName);
    }

    [Fact]
    public async Task BasketVariants_AreNeverSuggested()
    {
        _ruleRepository.Add(RecommendationRule.Create(null, new ProductVariantId(_burgerVariantId), priority: 1));
        _ruleRepository.Add(RecommendationRule.Create(null, new ProductVariantId(_colaVariantId), priority: 2));

        var results = await CreateHandler().Handle(
            new GetBasketRecommendationsQuery(null, [_burgerVariantId], DateTime.Today.AddHours(12), 3),
            CancellationToken.None);

        Assert.Equal([_colaVariantId], [.. results.Select(r => r.VariantId)]);
    }

    [Fact]
    public async Task NonMatchingEarlierRule_DoesNotSuppressMatchingRuleForSameVariant()
    {
        _ruleRepository.Add(RecommendationRule.Create(FriesProductId, new ProductVariantId(_colaVariantId), priority: 1));
        _ruleRepository.Add(RecommendationRule.Create(BurgerProductId, new ProductVariantId(_colaVariantId), priority: 2));
        var results = await CreateHandler().Handle(
            new GetBasketRecommendationsQuery(null, [_burgerVariantId], DateTime.Today.AddHours(12), int.MaxValue),
            CancellationToken.None);
        Assert.Equal([_colaVariantId], results.Select(r => r.VariantId));
    }

    [Fact]
    public async Task InactiveRules_AreExcluded()
    {
        var rule = RecommendationRule.Create(null, new ProductVariantId(_colaVariantId), priority: 1);
        rule.SetStatus(false);
        _ruleRepository.Add(rule);

        var results = await CreateHandler().Handle(
            new GetBasketRecommendationsQuery(null, [_burgerVariantId], DateTime.Today.AddHours(12), 3),
            CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task TimeWindow_IsRespected()
    {
        _ruleRepository.Add(RecommendationRule.Create(null, new ProductVariantId(_colaVariantId), priority: 1, startTime: new TimeSpan(18, 0, 0), endTime: new TimeSpan(23, 0, 0)));

        var handler = CreateHandler();
        var midday = await handler.Handle(new GetBasketRecommendationsQuery(null, [], DateTime.Today.AddHours(12), 3), CancellationToken.None);
        var evening = await handler.Handle(new GetBasketRecommendationsQuery(null, [], DateTime.Today.AddHours(19), 3), CancellationToken.None);

        Assert.Empty(midday);
        Assert.Equal([_colaVariantId], [.. evening.Select(r => r.VariantId)]);
    }

    [Fact]
    public async Task DayOfWeekMask_IsRespected()
    {
        // Wednesday only.
        var wednesday = new DateTime(2026, 9, 16, 12, 0, 0);
        var rule = RecommendationRule.Create(null, new ProductVariantId(_colaVariantId), priority: 1, daysOfWeek: 1 << (int)DayOfWeek.Wednesday);
        _ruleRepository.Add(rule);

        var handler = CreateHandler();
        var tuesday = await handler.Handle(new GetBasketRecommendationsQuery(null, [], wednesday.AddDays(-1), 3), CancellationToken.None);
        var onWednesday = await handler.Handle(new GetBasketRecommendationsQuery(null, [], wednesday), CancellationToken.None);

        Assert.Empty(tuesday);
        Assert.Equal([_colaVariantId], [.. onWednesday.Select(r => r.VariantId)]);
    }

    [Fact]
    public async Task PopularToday_FillsRemainingSlots_ExcludingBasketAndRuleMatches()
    {
        await AddCompletedOrderTodayAsync((_colaVariantId, 2m), (_friesVariantId, 1m));

        var results = await CreateHandler().Handle(
            new GetBasketRecommendationsQuery(null, [_burgerVariantId], DateTime.Now, 2),
            CancellationToken.None);

        Assert.Equal([_colaVariantId, _friesVariantId], [.. results.Select(r => r.VariantId)]);
        Assert.All(results, r => Assert.Equal(RecommendationReason.PopularToday, r.Reason));
    }

    [Fact]
    public async Task RulesTakePrecedence_ThenPopularTodayFills()
    {
        _ruleRepository.Add(RecommendationRule.Create(null, new ProductVariantId(_shakeVariantId), priority: 1));
        await AddCompletedOrderTodayAsync((_colaVariantId, 2m));

        var results = await CreateHandler().Handle(
            new GetBasketRecommendationsQuery(null, [_burgerVariantId], DateTime.Now, 2),
            CancellationToken.None);

        Assert.Equal(RecommendationReason.ConfiguredRule, results.ToList()[0].Reason);
        Assert.Equal(_shakeVariantId, results.ToList()[0].VariantId);
        Assert.Equal(RecommendationReason.PopularToday, results.ToList()[1].Reason);
        Assert.Equal(_colaVariantId, results.ToList()[1].VariantId);
    }

    [Fact]
    public async Task TakeZero_ReturnsEmpty()
    {
        var results = await CreateHandler().Handle(
            new GetBasketRecommendationsQuery(null, [_burgerVariantId], DateTime.Now, 0),
            CancellationToken.None);

        Assert.Empty(results);
    }

    private async Task AddCompletedOrderTodayAsync(params (Guid VariantId, decimal Quantity)[] lines)
    {
        var order = Order.Create(OrderType.TakeAway, new WarehouseId(Guid.NewGuid()));
        foreach (var (variantId, quantity) in lines)
        {
            var line = OrderLine.Create(order.Id, new ProductVariantId(variantId), quantity, 100m, 0m, true);
            order.AddOrderLine(line.Id);
            await _orderLineRepository.AddAsync(line);
        }

        order.Complete();
        await _orderRepository.AddAsync(order);
    }
}
