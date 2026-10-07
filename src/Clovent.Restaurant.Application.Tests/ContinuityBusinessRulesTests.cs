using Clovent.Restaurant.Application.Continuity;
using Xunit;

namespace Clovent.Restaurant.Application.Tests;

public sealed class ContinuityBusinessRulesTests
{
    [Theory]
    [InlineData("Cash")]
    [InlineData("cash")]
    [InlineData("Cash Payment")]
    [InlineData("EXACT CASH")]
    public void CanAcceptTender_Cash_Allowed(string paymentMethod)
    {
        var allowed = ContinuityBusinessRules.CanAcceptTender(paymentMethod, out var reason);
        Assert.True(allowed);
        Assert.Empty(reason);
    }

    [Theory]
    [InlineData("Card")]
    [InlineData("Credit Card")]
    [InlineData("Debit Card")]
    [InlineData("Visa")]
    [InlineData("MasterCard")]
    [InlineData("Customer Account")]
    [InlineData("On Account")]
    [InlineData("Voucher")]
    [InlineData(null)]
    [InlineData("")]
    public void CanAcceptTender_NonCash_Rejected(string? paymentMethod)
    {
        var allowed = ContinuityBusinessRules.CanAcceptTender(paymentMethod, out var reason);
        Assert.False(allowed);
        Assert.NotEmpty(reason);
    }

    [Fact]
    public void CanAcceptCardPayment_NotApproved_Rejected()
    {
        var allowed = ContinuityBusinessRules.CanAcceptCardPayment(false, out var reason);
        Assert.False(allowed);
        Assert.Contains("strictly prohibited", reason);
    }

    [Fact]
    public void CanAcceptCardPayment_ApprovedStoreAndForward_Allowed()
    {
        var allowed = ContinuityBusinessRules.CanAcceptCardPayment(true, out var reason);
        Assert.True(allowed);
        Assert.Empty(reason);
    }

    [Fact]
    public void CanAcceptOnAccount_AlwaysRejected()
    {
        var allowed = ContinuityBusinessRules.CanAcceptOnAccount(out var reason);
        Assert.False(allowed);
        Assert.NotEmpty(reason);
    }

    [Fact]
    public void CanPerformRefund_AlwaysRejected()
    {
        var allowed = ContinuityBusinessRules.CanPerformRefund(out var reason);
        Assert.False(allowed);
        Assert.NotEmpty(reason);
    }

    [Fact]
    public void CanVoidSettledTransaction_PriorOnline_Rejected()
    {
        var allowed = ContinuityBusinessRules.CanVoidSettledTransaction(true, out var reason);
        Assert.False(allowed);
        Assert.NotEmpty(reason);
    }

    [Fact]
    public void CanVoidSettledTransaction_NotPriorOnline_Allowed()
    {
        var allowed = ContinuityBusinessRules.CanVoidSettledTransaction(false, out var reason);
        Assert.True(allowed);
        Assert.Empty(reason);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void CanOverridePrice_GovernedByManagerPrivilege(bool hasManager, bool expectedResult)
    {
        var allowed = ContinuityBusinessRules.CanOverridePrice(hasManager, out var reason);
        Assert.Equal(expectedResult, allowed);
        if (expectedResult)
        {
            Assert.Empty(reason);
        }
        else
        {
            Assert.NotEmpty(reason);
        }
    }

    [Theory]
    [InlineData(10, false, 15, true)]
    [InlineData(15, false, 15, true)]
    [InlineData(20, false, 15, false)]
    [InlineData(20, true, 15, true)]
    [InlineData(50, true, 15, true)]
    public void CanApplyDiscount_PolicyThresholds_Enforced(
        decimal discountPct, bool hasManager, decimal maxCashierPct, bool expectedResult)
    {
        var allowed = ContinuityBusinessRules.CanApplyDiscount(discountPct, hasManager, maxCashierPct, out var reason);
        Assert.Equal(expectedResult, allowed);
        if (expectedResult)
        {
            Assert.Empty(reason);
        }
        else
        {
            Assert.NotEmpty(reason);
        }
    }

    [Fact]
    public void MasterDataAndInventoryModifications_StrictlyRejected()
    {
        Assert.False(ContinuityBusinessRules.CanCreateCustomer(out _));
        Assert.False(ContinuityBusinessRules.CanEditCustomer(out _));
        Assert.False(ContinuityBusinessRules.CanAdjustInventory(out _));
        Assert.False(ContinuityBusinessRules.CanModifyMasterData(out _));
    }
}
