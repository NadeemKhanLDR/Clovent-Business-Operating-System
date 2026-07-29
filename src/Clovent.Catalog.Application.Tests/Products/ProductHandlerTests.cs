using Clovent.Catalog.Application.Products.Commands;
using Clovent.Catalog.Application.Products.Queries;
using Clovent.Catalog.Application.Tests.TestSupport;
using Clovent.Catalog.Products;
using Clovent.Catalog.Products.ValueObjects;
using Clovent.Catalog.Shared.ValueObjects;
using Clovent.Catalog.UnitsOfMeasure;
using Xunit;

namespace Clovent.Catalog.Application.Tests.Products;

public class ProductHandlerTests
{
    [Fact]
    public async Task CreateProductCommandHandler_ValidRequest_PersistsAndReturnsDto()
    {
        var repository = new FakeProductRepository();
        var handler = new CreateProductCommandHandler(repository);

        var dto = await handler.Handle(new CreateProductCommand("Espresso Beans", "ESP-1KG", Guid.NewGuid()), CancellationToken.None);

        Assert.Equal("Espresso Beans", dto.Name);
        Assert.Equal("ESP-1KG", dto.Sku);
        Assert.NotNull(await repository.GetByIdAsync(new ProductId(dto.ProductId)));
    }

    [Fact]
    public async Task SetProductCategoryCommandHandler_UnknownProduct_Throws()
    {
        var handler = new SetProductCategoryCommandHandler(new FakeProductRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new SetProductCategoryCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task SetProductTaxConfigurationCommandHandler_UpdatesTaxConfiguration()
    {
        var repository = new FakeProductRepository();
        var product = Product.Create(ProductName.Create("Widget"), Sku.Create("WID-1"), UnitOfMeasureId.New());
        repository.Add(product);
        var handler = new SetProductTaxConfigurationCommandHandler(repository);

        var dto = await handler.Handle(new SetProductTaxConfigurationCommand(product.Id.Value, 15m, true), CancellationToken.None);

        Assert.Equal(15m, dto.TaxRatePercentage);
        Assert.True(dto.TaxIsInclusive);
    }

    [Fact]
    public async Task ActivateAndDeactivateProductCommandHandlers_RoundTrip()
    {
        var repository = new FakeProductRepository();
        var product = Product.Create(ProductName.Create("Widget"), Sku.Create("WID-2"), UnitOfMeasureId.New());
        product.Deactivate();
        repository.Add(product);

        var activated = await new ActivateProductCommandHandler(repository)
            .Handle(new ActivateProductCommand(product.Id.Value), CancellationToken.None);
        Assert.Equal("Active", activated.Status);

        var deactivated = await new DeactivateProductCommandHandler(repository)
            .Handle(new DeactivateProductCommand(product.Id.Value), CancellationToken.None);
        Assert.Equal("Inactive", deactivated.Status);
    }

    [Fact]
    public async Task GetProductBySkuQueryHandler_UnknownSku_Throws()
    {
        var handler = new GetProductBySkuQueryHandler(new FakeProductRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetProductBySkuQuery("UNKNOWN-SKU"), CancellationToken.None));
    }

    [Fact]
    public async Task ListProductsQueryHandler_ReturnsEveryProduct()
    {
        var repository = new FakeProductRepository();
        repository.Add(Product.Create(ProductName.Create("Product A"), Sku.Create("SKU-A"), UnitOfMeasureId.New()));
        repository.Add(Product.Create(ProductName.Create("Product B"), Sku.Create("SKU-B"), UnitOfMeasureId.New()));
        var handler = new ListProductsQueryHandler(repository);

        var result = await handler.Handle(new ListProductsQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
    }
}
