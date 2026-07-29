using Clovent.Restaurant.Infrastructure.Repositories;
using Clovent.Restaurant.Infrastructure.Tests.TestSupport;
using Clovent.Restaurant.Orders;
using Clovent.Restaurant.ServiceCharges;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.Repositories;

public class ServiceChargeRepositoryTests : SqliteTestBase
{
    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsFields()
    {
        var orderId = OrderId.New();
        var charge = ServiceCharge.Create(orderId, ServiceChargeType.Percentage, 12m, "Large party gratuity");

        await using (var writeContext = CreateContext())
        {
            var repository = new ServiceChargeRepository(writeContext);
            await repository.AddAsync(charge);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new ServiceChargeRepository(readContext).GetByIdAsync(charge.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(orderId, reloaded!.OrderId);
        Assert.Equal(ServiceChargeType.Percentage, reloaded.ServiceChargeType);
        Assert.Equal(12m, reloaded.Value);
    }

    [Fact]
    public async Task GetByOrderIdAsync_FiltersToOwningOrder()
    {
        var orderId = OrderId.New();

        await using (var writeContext = CreateContext())
        {
            var repository = new ServiceChargeRepository(writeContext);
            await repository.AddAsync(ServiceCharge.Create(orderId, ServiceChargeType.Percentage, 10m, "A"));
            await repository.AddAsync(ServiceCharge.Create(OrderId.New(), ServiceChargeType.Percentage, 10m, "B"));
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var found = await new ServiceChargeRepository(readContext).GetByOrderIdAsync(orderId);

        Assert.Single(found);
    }

    [Fact]
    public async Task GetByIdAsync_NoMatch_ReturnsNull()
    {
        await using var context = CreateContext();
        var result = await new ServiceChargeRepository(context).GetByIdAsync(ServiceChargeId.New());

        Assert.Null(result);
    }
}
