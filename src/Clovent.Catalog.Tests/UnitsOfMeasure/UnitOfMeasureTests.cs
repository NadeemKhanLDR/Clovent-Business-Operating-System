using Clovent.Catalog.Shared;
using Clovent.Catalog.UnitsOfMeasure;
using Clovent.Catalog.UnitsOfMeasure.Events;
using Clovent.Catalog.UnitsOfMeasure.ValueObjects;
using Xunit;

namespace Clovent.Catalog.Tests.UnitsOfMeasure;

public class UnitOfMeasureTests
{
    [Fact]
    public void Create_Valid_ActiveByDefault_RaisesUnitOfMeasureCreated()
    {
        var unit = UnitOfMeasure.Create(UnitOfMeasureCode.Create("kg"), "Kilogram");

        Assert.Equal("KG", unit.Code.Value);
        Assert.Equal("Kilogram", unit.Name);
        Assert.Equal(CatalogStatus.Active, unit.Status);
        Assert.IsType<UnitOfMeasureCreated>(Assert.Single(unit.DomainEvents));
    }

    [Fact]
    public void Rename_DifferentName_RaisesUnitOfMeasureRenamed()
    {
        var unit = UnitOfMeasure.Create(UnitOfMeasureCode.Create("KG"), "Kilogram");
        unit.ClearDomainEvents();

        unit.Rename("Kilograms");

        Assert.IsType<UnitOfMeasureRenamed>(Assert.Single(unit.DomainEvents));
    }

    [Fact]
    public void Deactivate_AlreadyInactive_Throws()
    {
        var unit = UnitOfMeasure.Create(UnitOfMeasureCode.Create("KG"), "Kilogram");
        unit.Deactivate();

        Assert.Throws<CatalogDomainException>(() => unit.Deactivate());
    }
}

public class UnitOfMeasureCodeTests
{
    [Theory]
    [InlineData("")]
    [InlineData("TOOLONGCODE1")]
    public void Create_Invalid_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => UnitOfMeasureCode.Create(value));
    }

    [Fact]
    public void Create_Valid_Normalizes()
    {
        Assert.Equal("PCS", UnitOfMeasureCode.Create("pcs").Value);
    }
}
