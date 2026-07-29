using Clovent.Catalog.Application.Tests.TestSupport;
using Clovent.Catalog.Application.Variants.Commands;
using Clovent.Catalog.Application.Variants.Queries;
using Clovent.Catalog.Products;
using Clovent.Catalog.Shared.ValueObjects;
using Clovent.Catalog.UnitsOfMeasure;
using Clovent.Catalog.Variants;
using Clovent.Catalog.Variants.ValueObjects;
using Xunit;

namespace Clovent.Catalog.Application.Tests.Variants;

public class ProductVariantHandlerTests
{
    [Fact]
    public async Task CreateProductVariantCommandHandler_ValidRequest_PersistsAndReturnsDto()
    {
        var repository = new FakeProductVariantRepository();
        var productId = ProductId.New();
        var handler = new CreateProductVariantCommandHandler(repository);

        var dto = await handler.Handle(new CreateProductVariantCommand(productId.Value, "Size: Large", "SKU-L", Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(productId.Value, dto.ProductId);
        Assert.NotNull(await repository.GetByIdAsync(new ProductVariantId(dto.ProductVariantId)));
    }

    [Fact]
    public async Task RenameProductVariantCommandHandler_UnknownVariant_Throws()
    {
        var handler = new RenameProductVariantCommandHandler(new FakeProductVariantRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new RenameProductVariantCommand(Guid.NewGuid(), "New Name"), CancellationToken.None));
    }

    [Fact]
    public async Task ActivateAndDeactivateProductVariantCommandHandlers_RoundTrip()
    {
        var repository = new FakeProductVariantRepository();
        var variant = ProductVariant.Create(ProductId.New(), VariantName.Create("Size: Large"), Sku.Create("SKU-X"), UnitOfMeasureId.New());
        variant.Deactivate();
        repository.Add(variant);

        var activated = await new ActivateProductVariantCommandHandler(repository)
            .Handle(new ActivateProductVariantCommand(variant.Id.Value), CancellationToken.None);
        Assert.Equal("Active", activated.Status);

        var deactivated = await new DeactivateProductVariantCommandHandler(repository)
            .Handle(new DeactivateProductVariantCommand(variant.Id.Value), CancellationToken.None);
        Assert.Equal("Inactive", deactivated.Status);
    }

    [Fact]
    public async Task ListProductVariantsByProductQueryHandler_FiltersToOwningProduct()
    {
        var repository = new FakeProductVariantRepository();
        var productId = ProductId.New();
        repository.Add(ProductVariant.Create(productId, VariantName.Create("Variant A"), Sku.Create("SKU-A"), UnitOfMeasureId.New()));
        repository.Add(ProductVariant.Create(ProductId.New(), VariantName.Create("Variant B"), Sku.Create("SKU-B"), UnitOfMeasureId.New()));
        var handler = new ListProductVariantsByProductQueryHandler(repository);

        var result = await handler.Handle(new ListProductVariantsByProductQuery(productId.Value), CancellationToken.None);

        Assert.Single(result);
    }
}
