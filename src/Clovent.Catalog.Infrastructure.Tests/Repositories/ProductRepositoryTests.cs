using Clovent.Catalog.Infrastructure.Repositories;
using Clovent.Catalog.Infrastructure.Tests.TestSupport;
using Clovent.Catalog.Products;
using Clovent.Catalog.Products.ValueObjects;
using Clovent.Catalog.Shared;
using Clovent.Catalog.Shared.ValueObjects;
using Clovent.Catalog.UnitsOfMeasure;
using Xunit;

namespace Clovent.Catalog.Infrastructure.Tests.Repositories;

public class ProductRepositoryTests : SqliteTestBase
{
    private static Product CreateProduct(string sku = "PRD-001", string name = "Espresso Beans 1kg") =>
        Product.Create(ProductName.Create(name), Sku.Create(sku), UnitOfMeasureId.New(), TaxConfiguration.Create(15m, false));

    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsFields()
    {
        var product = CreateProduct();

        await using (var writeContext = CreateContext())
        {
            var repository = new ProductRepository(writeContext);
            await repository.AddAsync(product);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new ProductRepository(readContext).GetByIdAsync(product.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(product.Name, reloaded!.Name);
        Assert.Equal(product.Sku, reloaded.Sku);
        Assert.Equal(product.BaseUnitOfMeasureId, reloaded.BaseUnitOfMeasureId);
        Assert.Equal(product.TaxConfiguration, reloaded.TaxConfiguration);
        Assert.Equal(CatalogStatus.Active, reloaded.Status);
    }

    [Fact]
    public async Task GetBySkuAsync_FindsMatch()
    {
        var product = CreateProduct(sku: "PRD-002");

        await using (var writeContext = CreateContext())
        {
            var repository = new ProductRepository(writeContext);
            await repository.AddAsync(product);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var found = await new ProductRepository(readContext).GetBySkuAsync(Sku.Create("PRD-002"));

        Assert.NotNull(found);
        Assert.Equal(product.Id, found!.Id);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEveryProduct()
    {
        await using (var writeContext = CreateContext())
        {
            var repository = new ProductRepository(writeContext);
            await repository.AddAsync(CreateProduct("PRD-003", "Product A"));
            await repository.AddAsync(CreateProduct("PRD-004", "Product B"));
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var all = await new ProductRepository(readContext).GetAllAsync();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task GetByIdAsync_NoMatch_ReturnsNull()
    {
        await using var context = CreateContext();
        var result = await new ProductRepository(context).GetByIdAsync(ProductId.New());

        Assert.Null(result);
    }
}
