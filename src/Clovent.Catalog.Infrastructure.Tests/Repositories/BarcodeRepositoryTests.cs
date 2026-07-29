using Clovent.Catalog.Barcodes;
using Clovent.Catalog.Barcodes.ValueObjects;
using Clovent.Catalog.Infrastructure.Repositories;
using Clovent.Catalog.Infrastructure.Tests.TestSupport;
using Clovent.Catalog.Shared;
using Clovent.Catalog.Variants;
using Xunit;

namespace Clovent.Catalog.Infrastructure.Tests.Repositories;

public class BarcodeRepositoryTests : SqliteTestBase
{
    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsFields()
    {
        var variantId = ProductVariantId.New();
        var barcode = Barcode.Create(variantId, BarcodeValue.Create("12345678"), isPrimary: true);

        await using (var writeContext = CreateContext())
        {
            var repository = new BarcodeRepository(writeContext);
            await repository.AddAsync(barcode);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new BarcodeRepository(readContext).GetByIdAsync(barcode.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(variantId, reloaded!.ProductVariantId);
        Assert.Equal(barcode.Value, reloaded.Value);
        Assert.True(reloaded.IsPrimary);
        Assert.Equal(CatalogStatus.Active, reloaded.Status);
    }

    [Fact]
    public async Task GetByValueAsync_FindsMatch()
    {
        var barcode = Barcode.Create(ProductVariantId.New(), BarcodeValue.Create("87654321"));

        await using (var writeContext = CreateContext())
        {
            var repository = new BarcodeRepository(writeContext);
            await repository.AddAsync(barcode);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var found = await new BarcodeRepository(readContext).GetByValueAsync(BarcodeValue.Create("87654321"));

        Assert.NotNull(found);
        Assert.Equal(barcode.Id, found!.Id);
    }

    [Fact]
    public async Task GetByProductVariantIdAsync_FiltersToOwningVariant()
    {
        var variantId = ProductVariantId.New();
        var otherVariantId = ProductVariantId.New();

        await using (var writeContext = CreateContext())
        {
            var repository = new BarcodeRepository(writeContext);
            await repository.AddAsync(Barcode.Create(variantId, BarcodeValue.Create("11111111")));
            await repository.AddAsync(Barcode.Create(variantId, BarcodeValue.Create("22222222")));
            await repository.AddAsync(Barcode.Create(otherVariantId, BarcodeValue.Create("33333333")));
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var found = await new BarcodeRepository(readContext).GetByProductVariantIdAsync(variantId);

        Assert.Equal(2, found.Count);
        Assert.All(found, b => Assert.Equal(variantId, b.ProductVariantId));
    }

    [Fact]
    public async Task GetByIdAsync_NoMatch_ReturnsNull()
    {
        await using var context = CreateContext();
        var result = await new BarcodeRepository(context).GetByIdAsync(BarcodeId.New());

        Assert.Null(result);
    }
}
