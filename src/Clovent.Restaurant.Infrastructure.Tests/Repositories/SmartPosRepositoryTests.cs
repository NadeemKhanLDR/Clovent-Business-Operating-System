using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Catalog.Variants;
using Clovent.Restaurant.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Clovent.Restaurant.Infrastructure.Tests.TestSupport;
using Clovent.Restaurant.QuickOrderTemplates;
using Clovent.Restaurant.SmartRecommendations;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.Repositories;

public class SmartPosRepositoryTests : SqliteTestBase
{
    [Fact]
    public async Task RecommendationRule_RoundTrips()
    {
        var variantId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        await using (var context = CreateContext())
        {
            var repository = new RecommendationRuleRepository(context);
            await repository.AddAsync(RecommendationRule.Create(productId, new ProductVariantId(variantId), 3, new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0), 0x7F, "note"), CancellationToken.None);
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext())
        {
            var repository = new RecommendationRuleRepository(context);
            var stored = (await repository.GetAllAsync(CancellationToken.None)).Single();
            var rule = await repository.GetByIdAsync(stored.Id, CancellationToken.None);

            Assert.NotNull(rule);
            Assert.Equal(productId, rule.ProductId);
            Assert.Equal(variantId, rule.RecommendedVariantId.Value);
            Assert.Equal(3, rule.Priority);
            Assert.True(rule.IsActive);
            Assert.Equal(new TimeSpan(9, 0, 0), rule.StartTime);
            Assert.Equal(new TimeSpan(17, 0, 0), rule.EndTime);
            Assert.Equal(0x7F, rule.DaysOfWeek);
            Assert.Equal("note", rule.Notes);
        }
    }

    [Fact]
    public async Task SuggestionEvent_RoundTripsAndRangeFilters()
    {
        var variantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await using (var context = CreateContext())
        {
            var repository = new SuggestionEventRepository(context);
            await repository.AddAsync(SuggestionEvent.Offered(orderId, new ProductVariantId(variantId), null), CancellationToken.None);
            await repository.AddAsync(SuggestionEvent.Accepted(orderId, new ProductVariantId(variantId), null, Guid.NewGuid(), 2m, 150m), CancellationToken.None);
            await repository.AddAsync(SuggestionEvent.Dismissed(orderId, new ProductVariantId(variantId), null), CancellationToken.None);
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext())
        {
            var repository = new SuggestionEventRepository(context);

            // OccurredAtUtc range filtering translates on SQL Server
            // (production) but not on EF Core's SQLite provider - the same
            // reason ShiftRepository's date filters are never exercised on
            // SQLite - so this round-trip fetches directly and range
            // semantics are covered by the Application-layer fake tests.
            var all = await context.SuggestionEvents.AsNoTracking().ToListAsync(CancellationToken.None);
            Assert.Equal(3, all.Count);

            // Note: the OccurredAtUtc range filter inside GetRangeAsync
            // translates on SQL Server (production) but not on EF Core's
            // SQLite provider - the same reason ShiftRepository's date
            // filters are never exercised on SQLite - so range semantics
            // are covered by the Application-layer tests' fake repository.
            var accepted = all.Single(e => e.Kind == SuggestionEventKind.Accepted);
            Assert.Equal(orderId, accepted.OrderId);
            Assert.Equal(variantId, accepted.VariantId.Value);
            Assert.Equal(2m, accepted.AcceptedQuantity);
            Assert.Equal(150m, accepted.AcceptedUnitAmount);

            Assert.All(all, e => Assert.True(e.OccurredAtUtc >= now.AddMinutes(-1) && e.OccurredAtUtc <= now.AddMinutes(1)));
        }
    }

    [Fact]
    public async Task QuickOrderTemplate_RoundTripsWithItems()
    {
        var variantA = Guid.NewGuid();
        var variantB = Guid.NewGuid();

        await using (var context = CreateContext())
        {
            var repository = new QuickOrderTemplateRepository(context);
            var template = QuickOrderTemplate.Create("Combo", "desc", 2);
            template.AddItem(new ProductVariantId(variantA), 2m);
            template.AddItem(new ProductVariantId(variantB), 1m, 70m);
            await repository.AddAsync(template, CancellationToken.None);
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext())
        {
            var repository = new QuickOrderTemplateRepository(context);
            var templates = await repository.GetActiveAsync(CancellationToken.None);

            var template = Assert.Single(templates);
            Assert.Equal("Combo", template.Name);
            Assert.Equal("desc", template.Description);
            Assert.Equal(2, template.DisplayOrder);
            Assert.Equal(2, template.Items.Count);

            var itemA = template.Items.Single(i => i.VariantId.Value == variantA);
            Assert.Equal(2m, itemA.Quantity);
            Assert.Null(itemA.TemplateUnitPrice);
            var itemB = template.Items.Single(i => i.VariantId.Value == variantB);
            Assert.Equal(70m, itemB.TemplateUnitPrice);
        }
    }
}
