using Clovent.Catalog.Products;
using Clovent.Catalog.Shared;
using Clovent.Catalog.Shared.ValueObjects;
using Clovent.Catalog.UnitsOfMeasure;
using Clovent.Catalog.Variants;
using Clovent.Catalog.Variants.Events;
using Clovent.Catalog.Variants.ValueObjects;
using Xunit;

namespace Clovent.Catalog.Tests.Variants;

public class ProductVariantTests
{
    [Fact]
    public void Create_Valid_ActiveByDefault_RaisesProductVariantCreated()
    {
        var productId = ProductId.New();

        var variant = ProductVariant.Create(productId, VariantName.Create("Size: Large"), Sku.Create("ESP-1KG-L"), UnitOfMeasureId.New());

        Assert.Equal(productId, variant.ProductId);
        Assert.Equal("Size: Large", variant.Name.Value);
        Assert.Equal(CatalogStatus.Active, variant.Status);
        Assert.IsType<ProductVariantCreated>(Assert.Single(variant.DomainEvents));
    }

    [Fact]
    public void Rename_DifferentName_RaisesProductVariantRenamed()
    {
        var variant = ProductVariant.Create(ProductId.New(), VariantName.Create("Size: Large"), Sku.Create("SKU-1"), UnitOfMeasureId.New());
        variant.ClearDomainEvents();

        variant.Rename(VariantName.Create("Size: XL"));

        Assert.IsType<ProductVariantRenamed>(Assert.Single(variant.DomainEvents));
    }

    [Fact]
    public void SetUnitOfMeasure_DifferentUnit_RaisesProductVariantUnitOfMeasureChanged()
    {
        var variant = ProductVariant.Create(ProductId.New(), VariantName.Create("Case of 12"), Sku.Create("SKU-2"), UnitOfMeasureId.New());
        variant.ClearDomainEvents();

        variant.SetUnitOfMeasure(UnitOfMeasureId.New());

        Assert.IsType<ProductVariantUnitOfMeasureChanged>(Assert.Single(variant.DomainEvents));
    }

    [Fact]
    public void Deactivate_AlreadyInactive_Throws()
    {
        var variant = ProductVariant.Create(ProductId.New(), VariantName.Create("Size: Large"), Sku.Create("SKU-3"), UnitOfMeasureId.New());
        variant.Deactivate();

        Assert.Throws<CatalogDomainException>(() => variant.Deactivate());
    }
}
