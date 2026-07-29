using Clovent.Catalog.Barcodes;
using Clovent.Catalog.Barcodes.Events;
using Clovent.Catalog.Barcodes.ValueObjects;
using Clovent.Catalog.Shared;
using Clovent.Catalog.Variants;
using Xunit;

namespace Clovent.Catalog.Tests.Barcodes;

public class BarcodeTests
{
    [Fact]
    public void Create_Valid_ActiveByDefault_RaisesBarcodeCreated()
    {
        var variantId = ProductVariantId.New();

        var barcode = Barcode.Create(variantId, BarcodeValue.Create("012345678905"));

        Assert.Equal(variantId, barcode.ProductVariantId);
        Assert.Equal("012345678905", barcode.Value.Value);
        Assert.False(barcode.IsPrimary);
        Assert.Equal(CatalogStatus.Active, barcode.Status);
        Assert.IsType<BarcodeCreated>(Assert.Single(barcode.DomainEvents));
    }

    [Fact]
    public void MarkAsPrimary_ThenUnmark_RoundTrips()
    {
        var barcode = Barcode.Create(ProductVariantId.New(), BarcodeValue.Create("012345678905"));
        barcode.ClearDomainEvents();

        barcode.MarkAsPrimary();
        Assert.True(barcode.IsPrimary);
        Assert.IsType<BarcodePrimaryChanged>(Assert.Single(barcode.DomainEvents));

        barcode.ClearDomainEvents();
        barcode.UnmarkAsPrimary();
        Assert.False(barcode.IsPrimary);
        Assert.IsType<BarcodePrimaryChanged>(Assert.Single(barcode.DomainEvents));
    }

    [Fact]
    public void MarkAsPrimary_AlreadyPrimary_IsNoOp()
    {
        var barcode = Barcode.Create(ProductVariantId.New(), BarcodeValue.Create("012345678905"), isPrimary: true);
        barcode.ClearDomainEvents();

        barcode.MarkAsPrimary();

        Assert.Empty(barcode.DomainEvents);
    }

    [Fact]
    public void Deactivate_AlreadyInactive_Throws()
    {
        var barcode = Barcode.Create(ProductVariantId.New(), BarcodeValue.Create("012345678905"));
        barcode.Deactivate();

        Assert.Throws<CatalogDomainException>(() => barcode.Deactivate());
    }
}

public class BarcodeValueTests
{
    [Theory]
    [InlineData("")]
    [InlineData("1234567")]
    [InlineData("123456789012345")]
    [InlineData("ABCDEFGH")]
    public void Create_Invalid_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => BarcodeValue.Create(value));
    }
}
