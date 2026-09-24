using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Clovent.Catalog.Variants;
using Clovent.Restaurant.Application.SmartRecommendations.Commands;
using Clovent.Restaurant.Application.SmartRecommendations.Queries;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Clovent.Restaurant.SmartRecommendations;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.SmartRecommendations;

public class SuggestionAnalyticsTests
{
    private static readonly Guid ProductAId = Guid.NewGuid();
    private static readonly Guid ProductBId = Guid.NewGuid();

    private readonly Guid _variantA = Guid.NewGuid();
    private readonly Guid _variantB = Guid.NewGuid();
    private readonly FakeSuggestionEventRepository _eventRepository = new();

    private RecordSuggestionEventCommandHandler CreateRecordHandler() => new(_eventRepository);
    private GetUpsellPerformanceQueryHandler CreatePerformanceHandler() =>
        new(
            _eventRepository,
            CatalogFakes.Mediator(
                new[]
                {
                    CatalogFakes.Variant(_variantA, ProductAId, "Item A"),
                    CatalogFakes.Variant(_variantB, ProductBId, "Item B"),
                },
                new[]
                {
                    CatalogFakes.Product(ProductAId, "Item A"),
                    CatalogFakes.Product(ProductBId, "Item B"),
                }));

    [Fact]
    public async Task Offered_Accepted_Dismissed_Events_ArePersistedWithTheirKind()
    {
        var orderId = Guid.NewGuid();
        var handler = CreateRecordHandler();

        await handler.Handle(new RecordSuggestionEventCommand(orderId, _variantA, null, SuggestionEventKind.Offered), CancellationToken.None);
        await handler.Handle(new RecordSuggestionEventCommand(orderId, _variantA, null, SuggestionEventKind.Accepted, Guid.NewGuid(), 2m, 150m), CancellationToken.None);
        await handler.Handle(new RecordSuggestionEventCommand(orderId, _variantB, _variantA, SuggestionEventKind.Dismissed), CancellationToken.None);

        Assert.Equal([SuggestionEventKind.Offered, SuggestionEventKind.Accepted, SuggestionEventKind.Dismissed], [.. _eventRepository.Events.Select(e => e.Kind)]);
        Assert.Equal(orderId, _eventRepository.Events[0].OrderId);
        Assert.Equal(_variantA, _eventRepository.Events[0].VariantId.Value);
        Assert.Equal(_variantA, _eventRepository.Events[2].TriggerVariantId);
    }

    [Fact]
    public async Task Performance_AggregatesOffersAcceptedDismissalsConversionAndRevenue()
    {
        var handler = CreateRecordHandler();
        var orderId = Guid.NewGuid();

        // Item A: offered 4 times, accepted twice (2 x 150 + 1 x 150), dismissed once.
        for (var i = 0; i < 4; i++)
        {
            await handler.Handle(new RecordSuggestionEventCommand(orderId, _variantA, null, SuggestionEventKind.Offered), CancellationToken.None);
        }
        await handler.Handle(new RecordSuggestionEventCommand(orderId, _variantA, null, SuggestionEventKind.Accepted, Guid.NewGuid(), 2m, 150m), CancellationToken.None);
        await handler.Handle(new RecordSuggestionEventCommand(orderId, _variantA, null, SuggestionEventKind.Accepted, Guid.NewGuid(), 1m, 150m), CancellationToken.None);
        await handler.Handle(new RecordSuggestionEventCommand(orderId, _variantA, null, SuggestionEventKind.Dismissed), CancellationToken.None);

        var rows = await CreatePerformanceHandler().Handle(
            new GetUpsellPerformanceQuery(DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddHours(1)),
            CancellationToken.None);

        var row = Assert.Single(rows);
        Assert.Equal("Item A", row.ProductName);
        Assert.Equal(4, row.Offers);
        Assert.Equal(2, row.Accepted);
        Assert.Equal(1, row.Dismissed);
        Assert.Equal(50m, row.ConversionPercent);
        Assert.Equal(450m, row.UpsellRevenue);
    }

    [Fact]
    public async Task Performance_ExcludesEventsOutsideTheQueriedInterval()
    {
        var handler = CreateRecordHandler();
        var orderId = Guid.NewGuid();
        await handler.Handle(new RecordSuggestionEventCommand(orderId, _variantA, null, SuggestionEventKind.Offered), CancellationToken.None);

        var rows = await CreatePerformanceHandler().Handle(
            new GetUpsellPerformanceQuery(DateTimeOffset.UtcNow.AddHours(2), DateTimeOffset.UtcNow.AddHours(3)),
            CancellationToken.None);

        Assert.Empty(rows);
    }

    [Fact]
    public async Task Performance_WithNoEvents_ReturnsEmpty()
    {
        var rows = await CreatePerformanceHandler().Handle(
            new GetUpsellPerformanceQuery(DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddHours(1)),
            CancellationToken.None);

        Assert.Empty(rows);
    }

    [Fact]
    public async Task ZeroOffers_NeverDividesByZero()
    {
        var handler = CreateRecordHandler();
        await handler.Handle(new RecordSuggestionEventCommand(Guid.NewGuid(), _variantA, null, SuggestionEventKind.Dismissed), CancellationToken.None);

        var row = Assert.Single(await CreatePerformanceHandler().Handle(
            new GetUpsellPerformanceQuery(DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow.AddHours(1)),
            CancellationToken.None));

        Assert.Equal(0m, row.ConversionPercent);
    }
}
