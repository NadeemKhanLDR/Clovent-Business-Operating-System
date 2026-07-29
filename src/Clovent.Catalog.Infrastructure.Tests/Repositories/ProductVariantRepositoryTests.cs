using Clovent.Catalog.Infrastructure.Repositories;
using Clovent.Catalog.Infrastructure.Tests.TestSupport;
using Clovent.Catalog.Products;
using Clovent.Catalog.Shared;
using Clovent.Catalog.Shared.ValueObjects;
using Clovent.Catalog.UnitsOfMeasure;
using Clovent.Catalog.Variants;
using Clovent.Catalog.Variants.ValueObjects;
using Xunit;

namespace Clovent.Catalog.Infrastructure.Tests.Repositories;

public class ProductVariantRepositoryTests : SqliteTestBase
{
    private static ProductVariant CreateVariant(ProductId productId, string sku = "PRD-001-A", string name = "Size: Large") =>
        ProductVariant.Create(productId, VariantName.Create(name), Sku.Create(sku), UnitOfMeasureId.New());

    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsFields()
    {
        var productId = ProductId.New();
        var variant = CreateVariant(productId);

        await using (var writeContext = CreateContext())
        {
            var repository = new ProductVariantRepository(writeContext);
            await repository.AddAsync(variant);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new ProductVariantRepository(readContext).GetByIdAsync(variant.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(productId, reloaded!.ProductId);
        Assert.Equal(variant.Name, reloaded.Name);
        Assert.Equal(variant.Sku, reloaded.Sku);
        Assert.Equal(CatalogStatus.Active, reloaded.Status);
    }

    [Fact]
    public async Task GetBySkuAsync_FindsMatch()
    {
        var variant = CreateVariant(ProductId.New(), sku: "PRD-002-A");

        await using (var writeContext = CreateContext())
        {
            var repository = new ProductVariantRepository(writeContext);
            await repository.AddAsync(variant);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var found = await new ProductVariantRepository(readContext).GetBySkuAsync(Sku.Create("PRD-002-A"));

        Assert.NotNull(found);
        Assert.Equal(variant.Id, found!.Id);
    }

    [Fact]
    public async Task GetByProductIdAsync_FiltersToOwningProduct()
    {
        var productId = ProductId.New();
        var otherProductId = ProductId.New();

        await using (var writeContext = CreateContext())
        {
            var repository = new ProductVariantRepository(writeContext);
            await repository.AddAsync(CreateVariant(productId, "PRD-003-A", "Variant A"));
            await repository.AddAsync(CreateVariant(productId, "PRD-003-B", "Variant B"));
            await repository.AddAsync(CreateVariant(otherProductId, "PRD-004-A", "Variant C"));
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var found = await new ProductVariantRepository(readContext).GetByProductIdAsync(productId);

        Assert.Equal(2, found.Count);
        Assert.All(found, v => Assert.Equal(productId, v.ProductId));
    }

    [Fact]
    public async Task GetByIdAsync_NoMatch_ReturnsNull()
    {
        await using var context = CreateContext();
        var result = await new ProductVariantRepository(context).GetByIdAsync(ProductVariantId.New());

        Assert.Null(result);
    }
}
