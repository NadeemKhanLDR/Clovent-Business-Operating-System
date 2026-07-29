using Clovent.Catalog.Categories;
using Clovent.Catalog.Categories.Events;
using Clovent.Catalog.Categories.ValueObjects;
using Clovent.Catalog.Shared;
using Xunit;

namespace Clovent.Catalog.Tests.Categories;

public class ProductCategoryTests
{
    [Fact]
    public void Create_Valid_ActiveByDefault_RaisesProductCategoryCreated()
    {
        var category = ProductCategory.Create(ProductCategoryName.Create("Beverages"));

        Assert.Equal("Beverages", category.Name.Value);
        Assert.Null(category.ParentCategoryId);
        Assert.Equal(CatalogStatus.Active, category.Status);
        Assert.IsType<ProductCategoryCreated>(Assert.Single(category.DomainEvents));
    }

    [Fact]
    public void Create_WithParent_SetsParentCategoryId()
    {
        var parentId = ProductCategoryId.New();

        var category = ProductCategory.Create(ProductCategoryName.Create("Soft Drinks"), parentId);

        Assert.Equal(parentId, category.ParentCategoryId);
    }

    [Fact]
    public void SetParent_ToSelf_Throws()
    {
        var category = ProductCategory.Create(ProductCategoryName.Create("Beverages"));

        Assert.Throws<CatalogDomainException>(() => category.SetParent(category.Id));
    }

    [Fact]
    public void Rename_DifferentName_RaisesProductCategoryRenamed()
    {
        var category = ProductCategory.Create(ProductCategoryName.Create("Beverages"));
        category.ClearDomainEvents();

        category.Rename(ProductCategoryName.Create("Drinks"));

        Assert.Equal("Drinks", category.Name.Value);
        Assert.IsType<ProductCategoryRenamed>(Assert.Single(category.DomainEvents));
    }

    [Fact]
    public void Deactivate_ThenDeactivateAgain_Throws()
    {
        var category = ProductCategory.Create(ProductCategoryName.Create("Beverages"));
        category.Deactivate();

        Assert.Throws<CatalogDomainException>(() => category.Deactivate());
    }

    [Fact]
    public void Activate_AlreadyActive_Throws()
    {
        var category = ProductCategory.Create(ProductCategoryName.Create("Beverages"));

        Assert.Throws<CatalogDomainException>(() => category.Activate());
    }
}
