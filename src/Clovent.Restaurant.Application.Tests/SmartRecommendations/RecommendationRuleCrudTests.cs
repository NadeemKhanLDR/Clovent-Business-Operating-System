using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Catalog.Variants;
using Clovent.Restaurant.Application.SmartRecommendations.Commands;
using Clovent.Restaurant.Application.SmartRecommendations.Dtos;
using Clovent.Restaurant.Application.SmartRecommendations.Queries;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.SmartRecommendations;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.SmartRecommendations;

public class RecommendationRuleCrudTests
{
    private readonly FakeRecommendationRuleRepository _repository = new();

    [Fact]
    public async Task Create_Valid_PersistsRule()
    {
        var variantId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var handler = new CreateRecommendationRuleCommandHandler(_repository);
        var result = await handler.Handle(
            new CreateRecommendationRuleCommand(productId, variantId, 5, new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0), 0x7F, "Upsell fries"),
            CancellationToken.None);

        Assert.Equal(productId, result.ProductId);
        Assert.Equal(variantId, result.RecommendedVariantId);
        Assert.Equal(5, result.Priority);
        Assert.True(result.IsActive);
        Assert.Equal(new TimeSpan(9, 0, 0), result.StartTime);
        var stored = Assert.Single(await _repository.GetAllAsync());
        Assert.Equal(result.RuleId, stored.Id.Value);
    }

    [Fact]
    public async Task Update_Existing_ChangesConfiguration()
    {
        var rule = RecommendationRule.Create(null, new ProductVariantId(Guid.NewGuid()), 1);
        await _repository.AddAsync(rule);

        var newVariant = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var handler = new UpdateRecommendationRuleCommandHandler(_repository);
        var result = await handler.Handle(
            new UpdateRecommendationRuleCommand(rule.Id.Value, productId, newVariant, 9, Notes: "updated"),
            CancellationToken.None);

        Assert.Equal(productId, result.ProductId);
        Assert.Equal(newVariant, result.RecommendedVariantId);
        Assert.Equal(9, result.Priority);
        Assert.Equal("updated", result.Notes);
    }

    [Fact]
    public async Task Update_MissingRule_ThrowsNotFound()
    {
        var handler = new UpdateRecommendationRuleCommandHandler(_repository);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateRecommendationRuleCommand(Guid.NewGuid(), null, Guid.NewGuid(), 1), CancellationToken.None));
    }

    [Fact]
    public async Task SetStatus_Deactivating_RemovesFromActiveSet()
    {
        var rule = RecommendationRule.Create(null, new ProductVariantId(Guid.NewGuid()), 1);
        await _repository.AddAsync(rule);

        var handler = new SetRecommendationRuleStatusCommandHandler(_repository);
        await handler.Handle(new SetRecommendationRuleStatusCommand(rule.Id.Value, false), CancellationToken.None);

        Assert.Empty(await _repository.GetActiveAsync());
        Assert.Single(await _repository.GetAllAsync());
    }

    [Fact]
    public async Task List_OrdersByPriority()
    {
        await _repository.AddAsync(RecommendationRule.Create(null, new ProductVariantId(Guid.NewGuid()), 3));
        await _repository.AddAsync(RecommendationRule.Create(null, new ProductVariantId(Guid.NewGuid()), 1));
        await _repository.AddAsync(RecommendationRule.Create(null, new ProductVariantId(Guid.NewGuid()), 2));

        var handler = new ListRecommendationRulesQueryHandler(_repository);
        var rules = await handler.Handle(new ListRecommendationRulesQuery(), CancellationToken.None);

        Assert.Equal([1, 2, 3], [.. rules.Select(r => r.Priority)]);
    }
}
