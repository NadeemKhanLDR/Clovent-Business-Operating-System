using Clovent.Restaurant.Application.Orders.Commands;
using Clovent.Restaurant.Application.Orders.Queries;
using Clovent.Restaurant.Application.Tests.TestSupport;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.Orders;

public class OrderNumberSequenceHandlerTests
{
    [Fact]
    public async Task GetOrderNumberSequenceQueryHandler_NoSequenceYet_ReturnsDefaults()
    {
        var handler = new GetOrderNumberSequenceQueryHandler(new FakeOrderNumberSequenceRepository());

        var result = await handler.Handle(new GetOrderNumberSequenceQuery(), CancellationToken.None);

        Assert.Equal("ORD-", result.Prefix);
        Assert.Equal(1, result.NextNumber);
    }

    [Fact]
    public async Task ConfigureOrderNumberSequenceCommandHandler_NoSequenceYet_CreatesAndConfigures()
    {
        var handler = new ConfigureOrderNumberSequenceCommandHandler(new FakeOrderNumberSequenceRepository());

        var result = await handler.Handle(new ConfigureOrderNumberSequenceCommand("INV-", 3453), CancellationToken.None);

        Assert.Equal("INV-", result.Prefix);
        Assert.Equal(3453, result.NextNumber);
    }

    [Fact]
    public async Task ConfigureOrderNumberSequenceCommandHandler_PersistsAcrossSubsequentGet()
    {
        var repository = new FakeOrderNumberSequenceRepository();
        var configureHandler = new ConfigureOrderNumberSequenceCommandHandler(repository);
        var getHandler = new GetOrderNumberSequenceQueryHandler(repository);

        await configureHandler.Handle(new ConfigureOrderNumberSequenceCommand("INV-", 3453), CancellationToken.None);
        var result = await getHandler.Handle(new GetOrderNumberSequenceQuery(), CancellationToken.None);

        Assert.Equal("INV-", result.Prefix);
        Assert.Equal(3453, result.NextNumber);
    }

    [Fact]
    public async Task ConfigureOrderNumberSequenceCommandHandler_InvalidPrefix_Throws()
    {
        var handler = new ConfigureOrderNumberSequenceCommandHandler(new FakeOrderNumberSequenceRepository());

        await Assert.ThrowsAsync<Clovent.Restaurant.RestaurantDomainException>(() =>
            handler.Handle(new ConfigureOrderNumberSequenceCommand("", 1), CancellationToken.None));
    }
}
