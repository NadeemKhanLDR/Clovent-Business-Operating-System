using Clovent.Restaurant.DomainServices;
using Xunit;

namespace Clovent.Restaurant.Tests.DomainServices;

public class PaymentMethodClassifierTests
{
    [Theory]
    [InlineData("Cash", true)]
    [InlineData("cash", true)]
    [InlineData("  CASH  ", true)]
    [InlineData("Card", false)]
    [InlineData("On Account", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void IsCash_ClassifiesConsistently(string? name, bool expected)
    {
        Assert.Equal(expected, PaymentMethodClassifier.IsCash(name));
    }

    [Theory]
    [InlineData("On Account", true)]
    [InlineData("on account", true)]
    [InlineData("  On Account  ", true)]
    [InlineData("Customer Account", true)]
    [InlineData("Credit", true)]
    [InlineData("Customer Credit", true)]
    [InlineData("Credit Card", false)]
    [InlineData("Debit Card", false)]
    [InlineData("Customer Advance", false)]
    [InlineData("Cash", false)]
    public void IsOnAccountCredit_ClassifiesAliasesAndIgnoresCards(string? name, bool expected)
    {
        Assert.Equal(expected, PaymentMethodClassifier.IsOnAccountCredit(name));
    }

    [Theory]
    [InlineData("Customer Advance", true)]
    [InlineData("  customer advance ", true)]
    [InlineData("Advance", true)]
    [InlineData("On Account", false)]
    [InlineData("Cash", false)]
    public void IsCustomerAdvance_ClassifiesConsistently(string? name, bool expected)
    {
        Assert.Equal(expected, PaymentMethodClassifier.IsCustomerAdvance(name));
    }

    [Theory]
    [InlineData("Card", true)]
    [InlineData("Credit Card", true)]
    [InlineData("  debit card  ", true)]
    [InlineData("MasterCard", true)]
    [InlineData("Visa Card", true)]
    [InlineData("Credit", false)]
    [InlineData("Customer Credit", false)]
    [InlineData("On Account", false)]
    [InlineData("Customer Advance", false)]
    [InlineData("Gift Card", false)]
    [InlineData("Gift Card Voucher", false)]
    [InlineData("Discarded", false)]
    [InlineData("Cardboard", false)]
    [InlineData("Easy Paisa", false)]
    [InlineData("Cash", false)]
    public void IsCard_ClassifiesCardTendersWithoutCollidingWithCreditOrGiftCards(string? name, bool expected)
    {
        Assert.Equal(expected, PaymentMethodClassifier.IsCard(name));
    }
}
