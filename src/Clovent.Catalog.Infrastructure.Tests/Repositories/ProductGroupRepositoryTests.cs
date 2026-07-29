using Clovent.Catalog.Groups;
using Clovent.Catalog.Groups.ValueObjects;
using Clovent.Catalog.Infrastructure.Repositories;
using Clovent.Catalog.Infrastructure.Tests.TestSupport;
using Clovent.Catalog.Shared;
using Xunit;

namespace Clovent.Catalog.Infrastructure.Tests.Repositories;

public class ProductGroupRepositoryTests : SqliteTestBase
{
    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsFields()
    {
        var group = ProductGroup.Create(ProductGroupName.Create("Soft Drinks"));

        await using (var writeContext = CreateContext())
        {
            var repository = new ProductGroupRepository(writeContext);
            await repository.AddAsync(group);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new ProductGroupRepository(readContext).GetByIdAsync(group.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(group.Name, reloaded!.Name);
        Assert.Equal(CatalogStatus.Active, reloaded.Status);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEveryGroup()
    {
        await using (var writeContext = CreateContext())
        {
            var repository = new ProductGroupRepository(writeContext);
            await repository.AddAsync(ProductGroup.Create(ProductGroupName.Create("Soft Drinks")));
            await repository.AddAsync(ProductGroup.Create(ProductGroupName.Create("Snacks")));
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var all = await new ProductGroupRepository(readContext).GetAllAsync();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task GetByIdAsync_NoMatch_ReturnsNull()
    {
        await using var context = CreateContext();
        var result = await new ProductGroupRepository(context).GetByIdAsync(ProductGroupId.New());

        Assert.Null(result);
    }
}
