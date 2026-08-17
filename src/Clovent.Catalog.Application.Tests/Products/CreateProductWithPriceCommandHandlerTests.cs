using Clovent.Catalog.Application.Products.Commands;
using Clovent.Catalog.Application.Tests.TestSupport;
using Clovent.Catalog.Prices;
using Clovent.Catalog.Products;
using Clovent.Catalog.Products.ValueObjects;
using Clovent.Catalog.Shared.ValueObjects;
using Clovent.Catalog.UnitsOfMeasure;
using Xunit;

namespace Clovent.Catalog.Application.Tests.Products;

public class CreateProductWithPriceCommandHandlerTests
{
    private static CreateProductWithPriceCommandHandler CreateHandler(
        out FakeProductRepository products,
        out FakeProductVariantRepository variants,
        out FakeProductPriceRepository prices)
    {
        products = new FakeProductRepository();
        variants = new FakeProductVariantRepository();
        prices = new FakeProductPriceRepository();
        return new CreateProductWithPriceCommandHandler(products, variants, prices);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesProductVariantAndPrice()
    {
        var handler = CreateHandler(out var products, out var variants, out var prices);
        var categoryId = Guid.NewGuid();
        var currencyId = Guid.NewGuid();
        var unitId = Guid.NewGuid();

        var dto = await handler.Handle(
            new CreateProductWithPriceCommand("Chicken Karahi", categoryId, 850m, currencyId, unitId),
            CancellationToken.None);

        Assert.Equal("Chicken Karahi", dto.Name);
        Assert.Equal(categoryId, dto.CategoryId);
        Assert.Equal("Active", dto.Status);

        var product = await products.GetByIdAsync(new ProductId(dto.ProductId));
        Assert.NotNull(product);

        var variant = (await variants.GetByProductIdAsync(new ProductId(dto.ProductId))).Single();
        Assert.Equal("Active", variant.Status.ToString());
        Assert.Equal(product!.Sku, variant.Sku);

        var price = (await prices.GetByProductVariantIdAsync(variant.Id)).Single();
        Assert.Equal(PriceType.Selling, price.PriceType);
        Assert.Equal(850m, price.Amount);
        Assert.Equal("Active", price.Status.ToString());
    }

    [Fact]
    public async Task Handle_InactiveRequest_DeactivatesBothProductAndVariant()
    {
        // RestaurantPosView filters product tiles on ProductVariant.Status, not
        // Product.Status - an "inactive" menu item must not still be sellable.
        var handler = CreateHandler(out var products, out var variants, out _);

        var dto = await handler.Handle(
            new CreateProductWithPriceCommand("Cold Drink", null, 120m, Guid.NewGuid(), Guid.NewGuid(), IsActive: false),
            CancellationToken.None);

        Assert.Equal("Inactive", dto.Status);
        var variant = (await variants.GetByProductIdAsync(new ProductId(dto.ProductId))).Single();
        Assert.Equal("Inactive", variant.Status.ToString());
    }

    [Fact]
    public async Task Handle_DuplicateName_GeneratesUniqueSkuSuffix()
    {
        var handler = CreateHandler(out var products, out _, out _);
        products.Add(Product.Create(ProductName.Create("Roti - existing"), Sku.Create("ROTI"), UnitOfMeasureId.New()));

        var dto = await handler.Handle(
            new CreateProductWithPriceCommand("Roti!!", null, 25m, Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.Equal("ROTI-2", dto.Sku);
    }
}
