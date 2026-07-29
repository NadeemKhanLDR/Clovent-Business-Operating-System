using Clovent.Catalog.Brands;
using Clovent.Catalog.Categories;
using Clovent.Catalog.Groups;
using Clovent.Catalog.Products;
using Clovent.Catalog.Products.Events;
using Clovent.Catalog.Products.ValueObjects;
using Clovent.Catalog.Shared;
using Clovent.Catalog.Shared.ValueObjects;
using Clovent.Catalog.UnitsOfMeasure;
using Xunit;

namespace Clovent.Catalog.Tests.Products;

public class ProductTests
{
    private static Product CreateProduct(string name = "Espresso Beans 1kg", string sku = "ESP-1KG") =>
        Product.Create(ProductName.Create(name), Sku.Create(sku), UnitOfMeasureId.New());

    [Fact]
    public void Create_Valid_ActiveByDefault_RaisesProductCreated()
    {
        var product = CreateProduct();

        Assert.Equal("Espresso Beans 1kg", product.Name.Value);
        Assert.Equal("ESP-1KG", product.Sku.Value);
        Assert.Equal(CatalogStatus.Active, product.Status);
        Assert.Equal(0m, product.TaxConfiguration.RatePercentage);
        Assert.IsType<ProductCreated>(Assert.Single(product.DomainEvents));
    }

    [Fact]
    public void Create_WithTaxConfiguration_SetsIt()
    {
        var tax = TaxConfiguration.Create(15m, isInclusive: true);

        var product = Product.Create(ProductName.Create("Widget"), Sku.Create("WID-1"), UnitOfMeasureId.New(), tax);

        Assert.Equal(tax, product.TaxConfiguration);
    }

    [Fact]
    public void SetCategory_ThenSetGroup_ThenSetBrand_UpdatesAll()
    {
        var product = CreateProduct();
        var categoryId = ProductCategoryId.New();
        var groupId = ProductGroupId.New();
        var brandId = BrandId.New();

        product.SetCategory(categoryId);
        product.SetGroup(groupId);
        product.SetBrand(brandId);

        Assert.Equal(categoryId, product.CategoryId);
        Assert.Equal(groupId, product.GroupId);
        Assert.Equal(brandId, product.BrandId);
    }

    [Fact]
    public void SetTaxConfiguration_DifferentValue_RaisesProductTaxConfigurationChanged()
    {
        var product = CreateProduct();
        product.ClearDomainEvents();

        product.SetTaxConfiguration(TaxConfiguration.Create(10m, false));

        Assert.IsType<ProductTaxConfigurationChanged>(Assert.Single(product.DomainEvents));
    }

    [Fact]
    public void Deactivate_ThenActivate_RoundTrips()
    {
        var product = CreateProduct();

        product.Deactivate();
        Assert.Equal(CatalogStatus.Inactive, product.Status);

        product.Activate();
        Assert.Equal(CatalogStatus.Active, product.Status);
    }

    [Fact]
    public void Activate_AlreadyActive_Throws()
    {
        var product = CreateProduct();

        Assert.Throws<CatalogDomainException>(() => product.Activate());
    }
}
