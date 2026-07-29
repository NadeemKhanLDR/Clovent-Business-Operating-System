using Clovent.MasterData;
using Clovent.MasterData.Currencies;
using Clovent.MasterData.Currencies.Events;
using Clovent.MasterData.Shared;
using Xunit;

namespace Clovent.MasterData.Tests.Currencies;

public class CurrencyTests
{
    [Fact]
    public void Create_Valid_ActiveByDefault_RaisesCurrencyCreated()
    {
        var currency = Currency.Create(CurrencyCode.Create("usd"), "US Dollar", "$", 2);

        Assert.Equal("USD", currency.Code.Value);
        Assert.Equal("US Dollar", currency.Name);
        Assert.Equal("$", currency.Symbol);
        Assert.Equal(2, currency.DecimalPlaces);
        Assert.Equal(MasterDataStatus.Active, currency.Status);
        Assert.IsType<CurrencyCreated>(Assert.Single(currency.DomainEvents));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    public void Create_InvalidDecimalPlaces_Throws(int decimalPlaces)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Currency.Create(CurrencyCode.Create("USD"), "US Dollar", "$", decimalPlaces));
    }

    [Fact]
    public void Create_ZeroDecimalPlaces_Allowed()
    {
        var currency = Currency.Create(CurrencyCode.Create("JPY"), "Japanese Yen", "¥", 0);

        Assert.Equal(0, currency.DecimalPlaces);
    }

    [Fact]
    public void Deactivate_AlreadyInactive_Throws()
    {
        var currency = Currency.Create(CurrencyCode.Create("USD"), "US Dollar", "$", 2);
        currency.Deactivate();

        Assert.Throws<MasterDataDomainException>(() => currency.Deactivate());
    }
}

public class CurrencyCodeTests
{
    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("12A")]
    public void Create_Invalid_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => CurrencyCode.Create(value));
    }

    [Fact]
    public void Create_Valid_Normalizes()
    {
        Assert.Equal("EUR", CurrencyCode.Create("eur").Value);
    }
}
