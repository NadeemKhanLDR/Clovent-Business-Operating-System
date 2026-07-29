using Clovent.Catalog.Infrastructure.Repositories;
using Clovent.Catalog.Infrastructure.Tests.TestSupport;
using Clovent.Catalog.Prices;
using Clovent.Catalog.Shared;
using Clovent.Catalog.Variants;
using Clovent.MasterData.Currencies;
using Xunit;

namespace Clovent.Catalog.Infrastructure.Tests.Repositories;

public class ProductPriceRepositoryTests : SqliteTestBase
{
    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsFields()
    {
        var variantId = ProductVariantId.New();
        var currencyId = CurrencyId.New();
        var price = ProductPrice.Create(variantId, PriceType.Selling, 9.99m, currencyId);

        await using (var writeContext = CreateContext())
        {
            var repository = new ProductPriceRepository(writeContext);
            await repository.AddAsync(price);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new ProductPriceRepository(readContext).GetByIdAsync(price.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(variantId, reloaded!.ProductVariantId);
        Assert.Equal(PriceType.Selling, reloaded.PriceType);
        Assert.Equal(price.Amount, reloaded.Amount);
        Assert.Equal(currencyId, reloaded.CurrencyId);
        Assert.Equal(CatalogStatus.Active, reloaded.Status);
    }

    [Fact]
    public async Task GetByProductVariantIdAsync_FiltersToOwningVariant()
    {
        var variantId = ProductVariantId.New();
        var otherVariantId = ProductVariantId.New();
        var currencyId = CurrencyId.New();

        await using (var writeContext = CreateContext())
        {
            var repository = new ProductPriceRepository(writeContext);
            await repository.AddAsync(ProductPrice.Create(variantId, PriceType.Cost, 5.00m, currencyId));
            await repository.AddAsync(ProductPrice.Create(variantId, PriceType.Selling, 9.99m, currencyId));
            await repository.AddAsync(ProductPrice.Create(otherVariantId, PriceType.Selling, 4.99m, currencyId));
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var found = await new ProductPriceRepository(readContext).GetByProductVariantIdAsync(variantId);

        Assert.Equal(2, found.Count);
        Assert.All(found, p => Assert.Equal(variantId, p.ProductVariantId));
    }

    [Fact]
    public async Task GetByIdAsync_NoMatch_ReturnsNull()
    {
        await using var context = CreateContext();
        var result = await new ProductPriceRepository(context).GetByIdAsync(ProductPriceId.New());

        Assert.Null(result);
    }
}
