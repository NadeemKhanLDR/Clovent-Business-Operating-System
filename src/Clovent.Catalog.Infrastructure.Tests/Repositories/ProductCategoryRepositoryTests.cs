using Clovent.Catalog.Categories;
using Clovent.Catalog.Categories.ValueObjects;
using Clovent.Catalog.Infrastructure.Repositories;
using Clovent.Catalog.Infrastructure.Tests.TestSupport;
using Clovent.Catalog.Shared;
using Xunit;

namespace Clovent.Catalog.Infrastructure.Tests.Repositories;

public class ProductCategoryRepositoryTests : SqliteTestBase
{
    [Fact]
    public async Task AddAsync_ThenGetById_RoundTripsFields()
    {
        var category = ProductCategory.Create(ProductCategoryName.Create("Beverages"));

        await using (var writeContext = CreateContext())
        {
            var repository = new ProductCategoryRepository(writeContext);
            await repository.AddAsync(category);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new ProductCategoryRepository(readContext).GetByIdAsync(category.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(category.Name, reloaded!.Name);
        Assert.Null(reloaded.ParentCategoryId);
        Assert.Equal(CatalogStatus.Active, reloaded.Status);
    }

    [Fact]
    public async Task AddAsync_WithParent_RoundTripsParentCategoryId()
    {
        var parent = ProductCategory.Create(ProductCategoryName.Create("Beverages"));
        var child = ProductCategory.Create(ProductCategoryName.Create("Soft Drinks"), parent.Id);

        await using (var writeContext = CreateContext())
        {
            var repository = new ProductCategoryRepository(writeContext);
            await repository.AddAsync(parent);
            await repository.AddAsync(child);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var reloaded = await new ProductCategoryRepository(readContext).GetByIdAsync(child.Id);

        Assert.NotNull(reloaded);
        Assert.Equal(parent.Id, reloaded!.ParentCategoryId);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEveryCategory()
    {
        await using (var writeContext = CreateContext())
        {
            var repository = new ProductCategoryRepository(writeContext);
            await repository.AddAsync(ProductCategory.Create(ProductCategoryName.Create("Beverages")));
            await repository.AddAsync(ProductCategory.Create(ProductCategoryName.Create("Snacks")));
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = CreateContext();
        var all = await new ProductCategoryRepository(readContext).GetAllAsync();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task GetByIdAsync_NoMatch_ReturnsNull()
    {
        await using var context = CreateContext();
        var result = await new ProductCategoryRepository(context).GetByIdAsync(ProductCategoryId.New());

        Assert.Null(result);
    }
}
