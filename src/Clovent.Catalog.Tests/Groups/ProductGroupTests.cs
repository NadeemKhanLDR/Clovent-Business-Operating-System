using Clovent.Catalog.Groups;
using Clovent.Catalog.Groups.Events;
using Clovent.Catalog.Groups.ValueObjects;
using Clovent.Catalog.Shared;
using Xunit;

namespace Clovent.Catalog.Tests.Groups;

public class ProductGroupTests
{
    [Fact]
    public void Create_Valid_ActiveByDefault_RaisesProductGroupCreated()
    {
        var group = ProductGroup.Create(ProductGroupName.Create("Soft Drinks"));

        Assert.Equal("Soft Drinks", group.Name.Value);
        Assert.Equal(CatalogStatus.Active, group.Status);
        Assert.IsType<ProductGroupCreated>(Assert.Single(group.DomainEvents));
    }

    [Fact]
    public void Rename_DifferentName_RaisesProductGroupRenamed()
    {
        var group = ProductGroup.Create(ProductGroupName.Create("Soft Drinks"));
        group.ClearDomainEvents();

        group.Rename(ProductGroupName.Create("Carbonated Drinks"));

        Assert.IsType<ProductGroupRenamed>(Assert.Single(group.DomainEvents));
    }

    [Fact]
    public void Deactivate_AlreadyInactive_Throws()
    {
        var group = ProductGroup.Create(ProductGroupName.Create("Soft Drinks"));
        group.Deactivate();

        Assert.Throws<CatalogDomainException>(() => group.Deactivate());
    }
}
