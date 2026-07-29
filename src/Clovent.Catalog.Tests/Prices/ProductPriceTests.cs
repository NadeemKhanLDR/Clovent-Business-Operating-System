using Clovent.Catalog.Prices;
using Clovent.Catalog.Prices.Events;
using Clovent.Catalog.Shared;
using Clovent.Catalog.Variants;
using Clovent.MasterData.Currencies;
using Xunit;

namespace Clovent.Catalog.Tests.Prices;

public class ProductPriceTests
{
    [Fact]
    public void Create_Valid_ActiveByDefault_RaisesProductPriceCreated()
    {
        var variantId = ProductVariantId.New();
        var currencyId = CurrencyId.New();

        var price = ProductPrice.Create(variantId, PriceType.Selling, 9.99m, currencyId);

        Assert.Equal(variantId, price.ProductVariantId);
        Assert.Equal(PriceType.Selling, price.PriceType);
        Assert.Equal(9.99m, price.Amount);
        Assert.Equal(currencyId, price.CurrencyId);
        Assert.Equal(CatalogStatus.Active, price.Status);
        Assert.IsType<ProductPriceCreated>(Assert.Single(price.DomainEvents));
    }

    [Fact]
    public void Create_NegativeAmount_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ProductPrice.Create(ProductVariantId.New(), PriceType.Cost, -1m, CurrencyId.New()));
    }

    [Fact]
    public void UpdateAmount_DifferentValue_RaisesProductPriceAmountChanged()
    {
        var price = ProductPrice.Create(ProductVariantId.New(), PriceType.Cost, 5m, CurrencyId.New());
        price.ClearDomainEvents();

        price.UpdateAmount(6m);

        Assert.Equal(6m, price.Amount);
        Assert.IsType<ProductPriceAmountChanged>(Assert.Single(price.DomainEvents));
    }

    [Fact]
    public void UpdateAmount_Negative_Throws()
    {
        var price = ProductPrice.Create(ProductVariantId.New(), PriceType.Cost, 5m, CurrencyId.New());

        Assert.Throws<ArgumentOutOfRangeException>(() => price.UpdateAmount(-1m));
    }

    [Fact]
    public void Deactivate_ThenActivate_RoundTrips()
    {
        var price = ProductPrice.Create(ProductVariantId.New(), PriceType.Selling, 9.99m, CurrencyId.New());

        price.Deactivate();
        Assert.Equal(CatalogStatus.Inactive, price.Status);

        price.Activate();
        Assert.Equal(CatalogStatus.Active, price.Status);
    }
}
