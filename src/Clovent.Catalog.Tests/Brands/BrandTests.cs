using Clovent.Catalog.Brands;
using Clovent.Catalog.Brands.Events;
using Clovent.Catalog.Brands.ValueObjects;
using Clovent.Catalog.Shared;
using Xunit;

namespace Clovent.Catalog.Tests.Brands;

public class BrandTests
{
    [Fact]
    public void Create_Valid_ActiveByDefault_RaisesBrandCreated()
    {
        var brand = Brand.Create(BrandName.Create("Acme"));

        Assert.Equal("Acme", brand.Name.Value);
        Assert.Equal(CatalogStatus.Active, brand.Status);
        Assert.IsType<BrandCreated>(Assert.Single(brand.DomainEvents));
    }

    [Fact]
    public void Rename_DifferentName_RaisesBrandRenamed()
    {
        var brand = Brand.Create(BrandName.Create("Acme"));
        brand.ClearDomainEvents();

        brand.Rename(BrandName.Create("Acme Corp"));

        Assert.IsType<BrandRenamed>(Assert.Single(brand.DomainEvents));
    }

    [Fact]
    public void Deactivate_AlreadyInactive_Throws()
    {
        var brand = Brand.Create(BrandName.Create("Acme"));
        brand.Deactivate();

        Assert.Throws<CatalogDomainException>(() => brand.Deactivate());
    }
}
