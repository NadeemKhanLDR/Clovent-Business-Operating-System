using Clovent.Restaurant.Application.Continuity;
using Xunit;

namespace Clovent.Restaurant.Application.Tests.Continuity;

public sealed class ContinuityBusinessRulesTests
{
    [Theory]
    [InlineData("Cash", true)]
    [InlineData("cash", true)]
    [InlineData("Cash Payment", true)]
    [InlineData("exact cash", true)]
    [InlineData("Credit Card", false)]
    [InlineData("Visa", false)]
    [InlineData("MasterCard", false)]
    [InlineData("Debit Card", false)]
    [InlineData("On Account", false)]
    [InlineData("Customer Credit", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void CanAcceptTender_EnforcesCashOnly(string? method, bool expectedAllowed)
    {
        var allowed = ContinuityBusinessRules.CanAcceptTender(method, out var reason);

        Assert.Equal(expectedAllowed, allowed);
        if (!expectedAllowed)
        {
            Assert.False(string.IsNullOrWhiteSpace(reason));
        }
    }

    [Fact]
    public void CanPerformRefund_AlwaysReturnsFalseWithRejectionMessage()
    {
        var allowed = ContinuityBusinessRules.CanPerformRefund(out var reason);

        Assert.False(allowed);
        Assert.Contains("prohibited", reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CanModifyMasterData_AlwaysReturnsFalseWithRejectionMessage()
    {
        var allowed = ContinuityBusinessRules.CanModifyMasterData(out var reason);

        Assert.False(allowed);
        Assert.Contains("cannot be performed", reason, StringComparison.OrdinalIgnoreCase);
    }
}
