using Clovent.Catalog.Brands;
using Clovent.Catalog.Brands.ValueObjects;
using Clovent.Catalog.Infrastructure.Repositories;
using Clovent.Catalog.Infrastructure.Tests.TestSupport;
using Clovent.Catalog.Shared;
using Xunit;

namespace Clovent.Catalog.Infrastructure.Tests.Repositories;

public class BrandRepositoryTests : SqliteTestBase
{
    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsFields()
    {
        var brand = Brand.Create(BrandName.Create("Acme"));

        await using (var writeContext = CreateContext())
        {
            var repository = new BrandRepository(writeContext);
            await repository.AddAsync(brand);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new BrandRepository(readContext).GetByIdAsync(brand.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(brand.Name, reloaded!.Name);
        Assert.Equal(CatalogStatus.Active, reloaded.Status);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEveryBrand()
    {
        await using (var writeContext = CreateContext())
        {
            var repository = new BrandRepository(writeContext);
            await repository.AddAsync(Brand.Create(BrandName.Create("Acme")));
            await repository.AddAsync(Brand.Create(BrandName.Create("Globex")));
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var all = await new BrandRepository(readContext).GetAllAsync();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task GetByIdAsync_NoMatch_ReturnsNull()
    {
        await using var context = CreateContext();
        var result = await new BrandRepository(context).GetByIdAsync(BrandId.New());

        Assert.Null(result);
    }
}
