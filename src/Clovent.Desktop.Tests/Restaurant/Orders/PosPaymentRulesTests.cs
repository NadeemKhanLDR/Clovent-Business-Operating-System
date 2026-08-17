using Clovent.Desktop.Restaurant.Orders;
using Xunit;

namespace Clovent.Desktop.Tests.Restaurant.Orders;

/// <summary>
/// Covers the tender-strip rules shared by <c>RestaurantPosForm</c>: which visual state a
/// payment-method button takes, and when recording a payment hands off to order completion.
/// </summary>
public class PosPaymentRulesTests
{
    [Fact]
    public void SelectedMethodResolvesToSelectedState()
    {
        Assert.Equal(
            PaymentMethodButtonState.Selected,
            PosPaymentRules.ResolveButtonState(isEnabled: true, isSelected: true));
    }

    [Fact]
    public void UnselectedMethodResolvesToUnselectedState()
    {
        Assert.Equal(
            PaymentMethodButtonState.Unselected,
            PosPaymentRules.ResolveButtonState(isEnabled: true, isSelected: false));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DisabledMethodStaysUnavailableRegardlessOfSelection(bool isSelected)
    {
        Assert.Equal(
            PaymentMethodButtonState.Unavailable,
            PosPaymentRules.ResolveButtonState(isEnabled: false, isSelected));
    }

    [Fact]
    public void OnlyOneMethodIsSelectedWhenSelectionMoves()
    {
        var methodIds = new[] { 1, 2, 3 };
        var selectedId = 2;

        var states = methodIds
            .Select(id => PosPaymentRules.ResolveButtonState(isEnabled: true, isSelected: id == selectedId))
            .ToList();

        Assert.Single(states, s => s == PaymentMethodButtonState.Selected);
        Assert.Equal(PaymentMethodButtonState.Selected, states[1]);
    }

    [Theory]
    [InlineData("Open")]
    [InlineData("Held")]
    public void AutoCompletesWhenBalanceIsFullyPaid(string status)
    {
        Assert.True(PosPaymentRules.ShouldAutoComplete(status, balanceAfterPayment: 0m, paymentRecorded: true));
    }

    [Fact]
    public void DoesNotAutoCompleteWhileBalanceRemains()
    {
        Assert.False(PosPaymentRules.ShouldAutoComplete("Open", balanceAfterPayment: 22.50m, paymentRecorded: true));
    }

    [Fact]
    public void DoesNotAutoCompleteWhenPaymentFailed()
    {
        // A failed payment leaves the server balance untouched; even at zero it must not complete.
        Assert.False(PosPaymentRules.ShouldAutoComplete("Open", balanceAfterPayment: 0m, paymentRecorded: false));
    }

    [Theory]
    [InlineData("Completed")]
    [InlineData("Voided")]
    [InlineData("Cancelled")]
    public void DoesNotCompleteAnOrderThatIsNoLongerOpen(string status)
    {
        Assert.False(PosPaymentRules.ShouldAutoComplete(status, balanceAfterPayment: 0m, paymentRecorded: true));
    }

    [Fact]
    public void DoesNotCompleteTwiceOnceStatusLeavesOpen()
    {
        // First settlement completes; re-running against the resulting status must not complete again.
        Assert.True(PosPaymentRules.ShouldAutoComplete("Open", 0m, paymentRecorded: true));
        Assert.False(PosPaymentRules.ShouldAutoComplete("Completed", 0m, paymentRecorded: true));
    }

    [Theory]
    [InlineData(0.004)]
    [InlineData(0.0)]
    [InlineData(-0.01)]
    public void TreatsSubCentAndOverpaidBalancesAsSettled(decimal balance)
    {
        Assert.True(PosPaymentRules.IsSettled(balance));
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(272.50)]
    public void TreatsRemainingBalanceAsUnsettled(decimal balance)
    {
        Assert.False(PosPaymentRules.IsSettled(balance));
    }
}
