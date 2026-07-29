using Clovent.Catalog.Variants;
using Clovent.Restaurant.Infrastructure.Repositories;
using Clovent.Restaurant.Infrastructure.Tests.TestSupport;
using Clovent.Restaurant.OrderLines;
using Clovent.Restaurant.Orders;
using Xunit;

namespace Clovent.Restaurant.Infrastructure.Tests.Repositories;

public class OrderLineRepositoryTests : SqliteTestBase
{
    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsFields()
    {
        var orderId = OrderId.New();
        var variantId = ProductVariantId.New();
        var line = OrderLine.Create(orderId, variantId, 2, 9.99m, 15m, false, "No ice");

        await using (var writeContext = CreateContext())
        {
            var repository = new OrderLineRepository(writeContext);
            await repository.AddAsync(line);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new OrderLineRepository(readContext).GetByIdAsync(line.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(orderId, reloaded!.OrderId);
        Assert.Equal(variantId, reloaded.ProductVariantId);
        Assert.Equal(2, reloaded.Quantity);
        Assert.Equal(9.99m, reloaded.UnitPrice);
        Assert.Equal(15m, reloaded.TaxRatePercentage);
        Assert.False(reloaded.TaxIsInclusive);
        Assert.Equal("No ice", reloaded.Notes);
        Assert.False(reloaded.IsVoided);
    }

    [Fact]
    public async Task GetByOrderIdAsync_FiltersToOwningOrder()
    {
        var orderId = OrderId.New();
        var otherOrderId = OrderId.New();

        await using (var writeContext = CreateContext())
        {
            var repository = new OrderLineRepository(writeContext);
            await repository.AddAsync(OrderLine.Create(orderId, ProductVariantId.New(), 1, 5m, 0, false));
            await repository.AddAsync(OrderLine.Create(orderId, ProductVariantId.New(), 1, 5m, 0, false));
            await repository.AddAsync(OrderLine.Create(otherOrderId, ProductVariantId.New(), 1, 5m, 0, false));
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var found = await new OrderLineRepository(readContext).GetByOrderIdAsync(orderId);

        Assert.Equal(2, found.Count);
        Assert.All(found, l => Assert.Equal(orderId, l.OrderId));
    }

    [Fact]
    public async Task GetByIdAsync_NoMatch_ReturnsNull()
    {
        await using var context = CreateContext();
        var result = await new OrderLineRepository(context).GetByIdAsync(OrderLineId.New());

        Assert.Null(result);
    }
}
