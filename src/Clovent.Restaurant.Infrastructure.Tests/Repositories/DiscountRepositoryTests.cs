using Clovent.Restaurant.Discounts;
using Clovent.Restaurant.Infrastructure.Repositories;
using Clovent.Restaurant.Infrastructure.Tests.TestSupport;
using Clovent.Restaurant.Orders;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.Repositories;

public class DiscountRepositoryTests : SqliteTestBase
{
    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsFields()
    {
        var orderId = OrderId.New();
        var discount = Discount.Create(orderId, DiscountType.Percentage, 10m, "Loyalty discount");

        await using (var writeContext = CreateContext())
        {
            var repository = new DiscountRepository(writeContext);
            await repository.AddAsync(discount);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new DiscountRepository(readContext).GetByIdAsync(discount.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(orderId, reloaded!.OrderId);
        Assert.Equal(DiscountType.Percentage, reloaded.DiscountType);
        Assert.Equal(10m, reloaded.Value);
        Assert.Equal("Loyalty discount", reloaded.Reason);
    }

    [Fact]
    public async Task GetByOrderIdAsync_FiltersToOwningOrder()
    {
        var orderId = OrderId.New();

        await using (var writeContext = CreateContext())
        {
            var repository = new DiscountRepository(writeContext);
            await repository.AddAsync(Discount.Create(orderId, DiscountType.Percentage, 10m, "A"));
            await repository.AddAsync(Discount.Create(OrderId.New(), DiscountType.Percentage, 10m, "B"));
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var found = await new DiscountRepository(readContext).GetByOrderIdAsync(orderId);

        Assert.Single(found);
    }

    [Fact]
    public async Task GetByIdAsync_NoMatch_ReturnsNull()
    {
        await using var context = CreateContext();
        var result = await new DiscountRepository(context).GetByIdAsync(DiscountId.New());

        Assert.Null(result);
    }
}
