using Clovent.Restaurant.Infrastructure.Repositories;
using Clovent.Restaurant.Infrastructure.Tests.TestSupport;
using Clovent.Restaurant.KitchenTickets;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.Repositories;

public class KitchenTicketRepositoryTests : SqliteTestBase
{
    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsFields()
    {
        var orderId = OrderId.New();
        var lineId = OrderLineId.New();
        var ticket = KitchenTicket.Create(orderId, [lineId]);

        await using (var writeContext = CreateContext())
        {
            var repository = new KitchenTicketRepository(writeContext);
            await repository.AddAsync(ticket);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new KitchenTicketRepository(readContext).GetByIdAsync(ticket.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(orderId, reloaded!.OrderId);
        Assert.Contains(lineId, reloaded.OrderLineIds);
        Assert.Equal(KitchenTicketStatus.New, reloaded.Status);
    }

    [Fact]
    public async Task GetByOrderIdAsync_FiltersToOwningOrder()
    {
        var orderId = OrderId.New();

        await using (var writeContext = CreateContext())
        {
            var repository = new KitchenTicketRepository(writeContext);
            await repository.AddAsync(KitchenTicket.Create(orderId, [OrderLineId.New()]));
            await repository.AddAsync(KitchenTicket.Create(OrderId.New(), [OrderLineId.New()]));
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var found = await new KitchenTicketRepository(readContext).GetByOrderIdAsync(orderId);

        Assert.Single(found);
    }

    [Fact]
    public async Task GetActiveAsync_ExcludesServedAndCancelled()
    {
        var active = KitchenTicket.Create(OrderId.New(), [OrderLineId.New()]);
        var served = KitchenTicket.Create(OrderId.New(), [OrderLineId.New()]);
        served.Start();
        served.MarkReady();
        served.Serve();

        await using (var writeContext = CreateContext())
        {
            var repository = new KitchenTicketRepository(writeContext);
            await repository.AddAsync(active);
            await repository.AddAsync(served);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var found = await new KitchenTicketRepository(readContext).GetActiveAsync();

        Assert.Single(found);
        Assert.Equal(active.Id, found.First().Id);
    }

    [Fact]
    public async Task GetByIdAsync_NoMatch_ReturnsNull()
    {
        await using var context = CreateContext();
        var result = await new KitchenTicketRepository(context).GetByIdAsync(KitchenTicketId.New());

        Assert.Null(result);
    }
}
