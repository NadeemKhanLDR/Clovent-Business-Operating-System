using Clovent.Catalog.Application.Categories.Commands;
using Clovent.Catalog.Application.Categories.Queries;
using Clovent.Catalog.Application.Tests.TestSupport;
using Clovent.Catalog.Categories;
using Clovent.Catalog.Categories.ValueObjects;
using Xunit;

namespace Clovent.Catalog.Application.Tests.Categories;

public class ProductCategoryHandlerTests
{
    [Fact]
    public async Task CreateProductCategoryCommandHandler_ValidRequest_PersistsAndReturnsDto()
    {
        var repository = new FakeProductCategoryRepository();
        var handler = new CreateProductCategoryCommandHandler(repository);

        var dto = await handler.Handle(new CreateProductCategoryCommand("Beverages"), CancellationToken.None);

        Assert.Equal("Beverages", dto.Name);
        Assert.NotNull(await repository.GetByIdAsync(new ProductCategoryId(dto.ProductCategoryId)));
    }

    [Fact]
    public async Task SetProductCategoryParentCommandHandler_ToSelf_Throws()
    {
        var repository = new FakeProductCategoryRepository();
        var category = ProductCategory.Create(ProductCategoryName.Create("Beverages"));
        repository.Add(category);
        var handler = new SetProductCategoryParentCommandHandler(repository);

        await Assert.ThrowsAsync<CatalogDomainException>(() =>
            handler.Handle(new SetProductCategoryParentCommand(category.Id.Value, category.Id.Value), CancellationToken.None));
    }

    [Fact]
    public async Task ActivateAndDeactivateProductCategoryCommandHandlers_RoundTrip()
    {
        var repository = new FakeProductCategoryRepository();
        var category = ProductCategory.Create(ProductCategoryName.Create("Beverages"));
        category.Deactivate();
        repository.Add(category);

        var activated = await new ActivateProductCategoryCommandHandler(repository)
            .Handle(new ActivateProductCategoryCommand(category.Id.Value), CancellationToken.None);
        Assert.Equal("Active", activated.Status);

        var deactivated = await new DeactivateProductCategoryCommandHandler(repository)
            .Handle(new DeactivateProductCategoryCommand(category.Id.Value), CancellationToken.None);
        Assert.Equal("Inactive", deactivated.Status);
    }

    [Fact]
    public async Task GetProductCategoryByIdQueryHandler_UnknownCategory_Throws()
    {
        var handler = new GetProductCategoryByIdQueryHandler(new FakeProductCategoryRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetProductCategoryByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task ListProductCategoriesQueryHandler_ReturnsEveryCategory()
    {
        var repository = new FakeProductCategoryRepository();
        repository.Add(ProductCategory.Create(ProductCategoryName.Create("Beverages")));
        repository.Add(ProductCategory.Create(ProductCategoryName.Create("Snacks")));
        var handler = new ListProductCategoriesQueryHandler(repository);

        var result = await handler.Handle(new ListProductCategoriesQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
    }
}
